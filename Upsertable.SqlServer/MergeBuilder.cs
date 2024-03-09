using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Upsertable.Extensions;

namespace Upsertable.SqlServer;

public class MergeBuilder(IEntityType type, DbContext db, Func<IEnumerable> provider)
{
    private readonly List<IMerge> _after = [];
    private readonly List<IMerge> _before = [];
    private readonly List<INavigation> _dependents = [];
    private readonly List<INavigation> _principals = [];
    private MergeBehavior _behavior;
    private IReadOnlyCollection<IPropertyBase>? _insert;
    private IDataLoader? _loader;
    private IReadOnlyCollection<IProperty>? _on;
    private bool _readonly;
    private IReadOnlyCollection<IPropertyBase>? _update;

    internal IEntityType EntityType => type;

    public MergeBuilder AsReadOnly(bool @readonly = true)
    {
        _readonly = @readonly;
        return this;
    }

    public MergeBuilder MergeBefore(IMerge merge)
    {
        _after.Add(merge);
        return this;
    }

    public MergeBuilder MergeAfter(IMerge merge)
    {
        _before.Add(merge);
        return this;
    }

    public MergeBuilder On(IReadOnlyCollection<IProperty> on)
    {
        _on = on;
        return this;
    }

    public MergeBuilder Insert(IReadOnlyCollection<IPropertyBase> insert)
    {
        _insert = insert;
        return this;
    }

    public MergeBuilder Update(IReadOnlyCollection<IPropertyBase> update)
    {
        _update = update;
        return this;
    }

    public MergeBuilder WithBehavior(MergeBehavior behavior, bool enable = true)
    {
        _behavior = enable ? _behavior | behavior : _behavior & ~behavior;
        return this;
    }

    public MergeBuilder WithPrincipal(INavigation navigation)
    {
        _principals.Add(navigation);
        return this;
    }

    public MergeBuilder WithDependent(INavigation navigation)
    {
        _dependents.Add(navigation);
        return this;
    }

    public MergeBuilder WithSourceLoader(IDataLoader loader)
    {
        _loader = loader;
        return this;
    }

    public Task ExecuteAsync(CancellationToken cancellation = default)
    {
        return ToMerge().ExecuteAsync(cancellation);
    }

    protected MergeBuilder Merge<TProperty>(LambdaExpression property, Action<MergeBuilder<TProperty>> build) where TProperty : class
    {
        var expression = property.Body as MemberExpression ?? throw new ArgumentException("Expression body must describe a field or property.");
        var navigation = EntityType.FindNavigationBase(expression.Member) ?? throw new ArgumentException("Expression body must describe a navigation property.");

        return Merge(navigation, build);
    }

    protected MergeBuilder Merge<TProperty>(INavigationBase property, Action<MergeBuilder<TProperty>> build) where TProperty : class
    {
        var builder = new MergeBuilder<TProperty>(db, () =>
        {
            var entities = new List<object>();

            foreach (var source in provider())
            {
                var value = property.GetValue(source);
                if (value == null) continue;

                if (property.IsCollection)
                    entities.AddRange(((ICollection)value).Cast<object>());
                else
                    entities.Add(value);
            }

            return entities.Distinct();
        });

        build(builder);

        switch (property)
        {
            case ISkipNavigation skip:
            {
#pragma warning disable EF1001 // Internal EF Core API usage.
                var source = db.GetDependencies().StateManager.EntityMaterializerSource;
                var context = new MaterializationContext(default, db);
#pragma warning restore EF1001 // Internal EF Core API usage.

                var joins =
                    new MergeBuilder(skip.JoinEntityType, db, () =>
                        {
                            var entities = new List<object>();

#pragma warning disable EF1001 // Internal EF Core API usage.
                            var materializer = skip.JoinEntityType.GetOrCreateMaterializer(source);
#pragma warning restore EF1001 // Internal EF Core API usage.

                            foreach (var data in provider())
                            {
                                var accessor = skip.GetCollectionAccessor() ?? throw new InvalidOperationException("Navigation must be a collection.");
                                var items = (IEnumerable)accessor.GetOrCreate(data, false);

                                foreach (var item in items)
                                {
                                    var entity = materializer(context);

                                    skip.ForeignKey.Properties.SetValues(entity, skip.ForeignKey.PrincipalKey.Properties.GetValues(data));
                                    skip.Inverse.ForeignKey.Properties.SetValues(entity, skip.Inverse.ForeignKey.PrincipalKey.Properties.GetValues(item));

                                    entities.Add(entity);
                                }
                            }

                            return entities.Distinct();
                        })
                        .WithBehavior(MergeBehavior.Insert)
                        .ToMerge();

                return skip.IsOnDependent
                    ? MergeAfter(builder.MergeBefore(joins).ToMerge())
                    : MergeBefore(builder.ToMerge()).MergeBefore(joins);
            }
            case INavigation navigation:
            {
                return navigation.IsOnDependent
                    ? MergeAfter(builder.ToMerge()).WithPrincipal(navigation)
                    : MergeBefore(builder.ToMerge()).WithDependent(navigation);
            }
            default:
                throw new NotSupportedException("Unknown navigation type.");
        }
    }

    public IMerge ToMerge()
    {
        var resolvers = db.GetService<IEnumerable<IDataResolver>>();
        var loader = _loader ?? db.GetService<IDataLoader>();

        var primary = EntityType.FindPrimaryKey() ?? throw new InvalidOperationException("Primary key must not be null.");
        var keys = primary.Properties;
        var navigations =
            from navigation in EntityType.GetNavigations()
            where !navigation.IsOnDependent
            where !navigation.IsCollection
            where navigation.TargetEntityType.IsOwned()
            select navigation;
        var properties = EntityType.GetProperties().Concat<IPropertyBase>(navigations).ToList();

        var source = new Source(db, properties, loader, resolvers);
        var on = _on ?? keys;
        var insert = _behavior.HasFlag(MergeBehavior.Insert) ? _insert ?? properties : default;
        var update = _behavior.HasFlag(MergeBehavior.Update) ? _update ?? properties : default;
        var output = new Output(db, on.Union(EntityType.GetKeys().SelectMany(key => key.Properties).Distinct()));
        var merge = new Merge(db, source, EntityType, output, provider) { Behavior = _behavior, IsReadOnly = _readonly };

        merge.On.AddRange(on);

        if (insert != null) merge.Insert.AddRange(insert);
        if (update != null) merge.Update.AddRange(update);

        merge.Principals.AddRange(_principals);
        merge.Dependents.AddRange(_dependents);

        return
            _before.Count > 0 || _after.Count > 0
                ? new MergeComposite(_before.Append(merge).Concat(_after))
                : merge;
    }
}