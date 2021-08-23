using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFramework.SqlMerge
{
    public class MergeBuilder : IMergeBuilder
    {
        private readonly DbContext _db;
        private readonly ICollection<IMergeBuilder> _dependents = new List<IMergeBuilder>();
        private readonly List<INavigationBase> _navigations = new();
        private readonly ICollection<IMergeBuilder> _principals = new List<IMergeBuilder>();

        private MergeBehavior _behavior;
        private MergeInsert _insert;
        private MergeOn _on;
        private IMergeSourceBuilder _sourceBuilder;
        private IMergeSourceLoader _sourceLoader;
        private MergeUpdate _update;

        public MergeBuilder(DbContext db, MergeContext context, IEntityType entityType)
        {
            _db = db;
            Context = context;
            EntityType = entityType;
        }

        internal MergeContext Context { get; }

        internal IEntityType EntityType { get; }

        public IMerge ToMerge()
        {
            var builder = _sourceBuilder ?? _db.GetService<IMergeSourceBuilder>();
            var loader = _sourceLoader ?? _db.GetService<IMergeSourceLoader>();
            var keys = EntityType.FindPrimaryKey().Properties;
            var navigations =
                from navigation in EntityType.GetNavigations()
                where !navigation.IsOnDependent
                where !navigation.IsCollection
                where navigation.TargetEntityType.IsOwned()
                select navigation;
            var properties = EntityType.GetProperties().Concat<IPropertyBase>(navigations).ToList();

            var target = new MergeTarget(EntityType);
            var source = new MergeSource(_db, properties, builder, loader);
            var on = _on ?? new MergeOn(keys);
            var insert = _behavior.HasFlag(MergeBehavior.WhenNotMatchedByTargetThenInsert) ? _insert ?? new MergeInsert(properties) : null;
            var update = _behavior.HasFlag(MergeBehavior.WhenMatchedThenUpdate) ? _update ?? new MergeUpdate(properties) : null;
            var output = new MergeOutput(_db, on.Properties.Union(EntityType.GetKeys().SelectMany(key => key.Properties).Distinct()));

            //if (EntityType.IsPropertyBag)
            //{
            //    // TODO var principalKeys = EntityType.GetForeignKeys().Select(key => key.PrincipalKey).ToList();
            //    // TODO delay, and iterate through all

            //    var bag = Activator.CreateInstance(EntityType.ClrType);

            //    var constituenciesId = EntityType.GetProperty("ConstituenciesId");
            //    var governmentsId = EntityType.GetProperty("GovernmentsId");

            //    constituenciesId.SetValue(bag, 5);
            //    governmentsId.SetValue(bag, 7);

            //    throw new NotSupportedException();
            //}

            foreach (var entity in Context.Get(EntityType.ClrType))
            foreach (var navigationBase in _navigations)
            {
                switch (navigationBase)
                {
                    case ISkipNavigation skipNavigation:
                    {
                        Context.AddRange(skipNavigation.TargetEntityType.ClrType, (ICollection) skipNavigation.GetCollectionAccessor().GetOrCreate(entity, false));
                        break;
                    }
                    case INavigation navigation:
                    {
                        var value = navigation.GetGetter().GetClrValue(entity);
                        if (value == null)
                            continue;

                        var type = navigation.TargetEntityType.ClrType;

                        if (navigation.IsCollection)
                            Context.AddRange(type, (IEnumerable) value);
                        else
                            Context.Add(type, value);
                        break;
                    }
                    default:
                        throw new NotSupportedException("Unknown navigation type.");
                }
            }

            var principals = _principals.Select(principal => principal.ToMerge());
            var dependents = _dependents.Select(dependent => dependent.ToMerge());
            var merge = new Merge(_db, target, source, on, _behavior, insert, update, output);

            return new MergeComposite(principals.Append(merge).Concat(dependents));
        }

        public MergeBuilder Using(IMergeSourceBuilder builder)
        {
            _sourceBuilder = builder;
            return this;
        }

        public MergeBuilder Using(IMergeSourceLoader loader)
        {
            _sourceLoader = loader;
            return this;
        }

        public MergeBuilder On(MergeOn on)
        {
            _on = on;
            return this;
        }

        public MergeBuilder WithBehavior(MergeBehavior behavior, bool enable = true)
        {
            _behavior = enable ? _behavior | behavior : _behavior & ~behavior;
            return this;
        }

        public MergeBuilder Insert(MergeInsert insert)
        {
            _insert = insert;
            return this;
        }

        public MergeBuilder Update(MergeUpdate update)
        {
            _update = update;
            return this;
        }

        protected MergeBuilder Merge<TProperty>(LambdaExpression property, Action<MergeBuilder<TProperty>> build) where TProperty : class
        {
            var navigationBase = property.Body is MemberExpression body ? EntityType.FindNavigation(body.Member) ?? EntityType.FindSkipNavigation(body.Member) as INavigationBase : default;
            if (navigationBase == null)
                throw new ArgumentException("Expression body must describe a navigation property.");

            var builder = new MergeBuilder<TProperty>(_db, Context);

            build(builder);

            switch (navigationBase)
            {
                case ISkipNavigation skipNavigation:
                {
                    var skip = new MergeBuilder(_db, Context, skipNavigation.JoinEntityType).WithBehavior(MergeBehavior.WhenNotMatchedByTargetThenInsert);

                    if (skipNavigation.IsOnDependent)
                        builder._dependents.Add(skip);
                    else
                        _dependents.Add(skip);

                    _principals.Add(builder);
                    break;
                }
                case INavigation navigation:
                {
                    if (navigation.IsOnDependent)
                        _principals.Add(builder);
                    else
                        _dependents.Add(builder);
                    break;
                }
                default:
                    throw new NotSupportedException("Unknown navigation type.");
            }

            _navigations.Add(navigationBase);

            return this;
        }

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return ToMerge().ExecuteAsync(Context, cancellationToken);
        }
    }

    public class MergeBuilder<T> : MergeBuilder where T : class
    {
        public MergeBuilder(DbContext db, MergeContext context)
            : base(db, context, db.Model.FindEntityType(typeof(T)))
        {
        }

        public MergeBuilder<T> Merge<TProperty>(Expression<Func<T, TProperty>> property, Action<MergeBuilder<TProperty>> build) where TProperty : class
        {
            return (MergeBuilder<T>) base.Merge(property, build);
        }

        public MergeBuilder<T> MergeMany<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> property, Action<MergeBuilder<TProperty>> build) where TProperty : class
        {
            return (MergeBuilder<T>) Merge(property, build);
        }

        public new MergeBuilder<T> Using(IMergeSourceBuilder builder)
        {
            return (MergeBuilder<T>) base.Using(builder);
        }

        public new MergeBuilder<T> Using(IMergeSourceLoader loader)
        {
            return (MergeBuilder<T>) base.Using(loader);
        }

        public new MergeBuilder<T> WithOn(MergeOn on)
        {
            return (MergeBuilder<T>) base.On(on);
        }

        public new MergeBuilder<T> WithBehavior(MergeBehavior behavior, bool enable = true)
        {
            return (MergeBuilder<T>) base.WithBehavior(behavior, enable);
        }

        public new MergeBuilder<T> WithInserts(MergeInsert insert)
        {
            return (MergeBuilder<T>) base.Insert(insert);
        }

        public new MergeBuilder<T> WithUpdates(MergeUpdate update)
        {
            return (MergeBuilder<T>) base.Update(update);
        }
    }
}