using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Upsertable.Data;

namespace Upsertable.SqlServer;

public class MergeBuilder
{
    private readonly List<IMerge> _after = new();
    private readonly List<IMerge> _before = new();
    private readonly DbContext _db;
    private readonly List<INavigation> _dependents = new();
    private readonly EntityProviderFunc _provider;
    private readonly List<INavigation> _principals = new();
    private MergeBehavior _behavior;
    private IReadOnlyCollection<IPropertyBase> _insert;
    private IDataLoader _loader;
    private IReadOnlyCollection<IProperty> _on;
    private bool _readonly;
    private IReadOnlyCollection<IPropertyBase> _update;

    public MergeBuilder(DbContext db, IEntityType entityType, EntityProviderFunc provider)
    {
        _db = db;
        EntityType = entityType;
        _provider = provider;
    }

    internal IEntityType EntityType { get; }

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

    protected MergeBuilder Merge<TProperty>(LambdaExpression property, Action<MergeBuilder<TProperty>> build) where TProperty : class
    {
        var navigationBase = property.Body is MemberExpression body ? EntityType.FindNavigation(body.Member) ?? EntityType.FindSkipNavigation(body.Member) as INavigationBase : default;
        if (navigationBase == null)
            throw new ArgumentException("Expression body must describe a navigation property.");

        var builder = new MergeBuilder<TProperty>(_db, _db.Model.FindEntityType(typeof(TProperty)), EntityProvider.Lazy(navigationBase, _provider));

        build(builder);

        switch (navigationBase)
        {
            case ISkipNavigation skipNavigation:
            {
                var joins =
                    new MergeBuilder(_db, skipNavigation.JoinEntityType, EntityProvider.Join(_db, skipNavigation, _provider))
                        .WithBehavior(MergeBehavior.Insert)
                        .ToMerge();

                return skipNavigation.IsOnDependent
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
        var resolvers = _db.GetService<IEnumerable<IDataResolver>>();
        var loader = _loader ?? _db.GetService<IDataLoader>();

        var primary = EntityType.FindPrimaryKey() ?? throw new InvalidOperationException("Primary key must not be null.");
        var keys = primary.Properties;
        var navigations =
            from navigation in EntityType.GetNavigations()
            where !navigation.IsOnDependent
            where !navigation.IsCollection
            where navigation.TargetEntityType.IsOwned()
            select navigation;
        var properties = EntityType.GetProperties().Concat<IPropertyBase>(navigations).ToList();

        var source = new Source(_db, properties, loader, resolvers);
        var on = _on ?? keys;
        var insert = _behavior.HasFlag(MergeBehavior.Insert) ? _insert ?? properties : default;
        var update = _behavior.HasFlag(MergeBehavior.Update) ? _update ?? properties : default;
        var output = new Output(_db, on.Union(EntityType.GetKeys().SelectMany(key => key.Properties).Distinct()));
        var merge = new Merge(_db, EntityType, source, output, _provider) { Behavior = _behavior, IsReadOnly = _readonly };

        merge.On.AddRange(on);

        if (insert != null) merge.Insert.AddRange(insert);
        if (update != null) merge.Update.AddRange(update);

        merge.Principals.AddRange(_principals);
        merge.Dependents.AddRange(_dependents);

        return
            _before.Any() || _after.Any()
                ? new MergeComposite(_before.Append(merge).Concat(_after))
                : merge;
    }
}