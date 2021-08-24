using System;
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
        private readonly ICollection<IMerge> _after = new List<IMerge>();
        private readonly ICollection<IMerge> _before = new List<IMerge>();
        private readonly DbContext _dbContext;
        private readonly IEntityResolver _entityResolver;
        private readonly IEntityType _entityType;
        private readonly List<INavigation> _principals = new();
        private readonly List<INavigation> _dependents = new();

        private MergeBehavior _behavior;
        private MergeInsert _insert;
        private MergeOn _on;
        private IMergeSourceBuilder _sourceBuilder;
        private IMergeSourceLoader _sourceLoader;
        private MergeUpdate _update;

        public MergeBuilder(DbContext dbContext, IEntityType entityType, IEntityResolver entityResolver)
        {
            _dbContext = dbContext;
            _entityType = entityType;
            _entityResolver = entityResolver;
        }

        // ReSharper disable once ConvertToAutoPropertyWhenPossible
        internal IEntityType EntityType => _entityType;

        public IMerge ToMerge()
        {
            var builder = _sourceBuilder ?? _dbContext.GetService<IMergeSourceBuilder>();
            var loader = _sourceLoader ?? _dbContext.GetService<IMergeSourceLoader>();

            var keys = _entityType.FindPrimaryKey().Properties;
            var navigations =
                from navigation in _entityType.GetNavigations()
                where !navigation.IsOnDependent
                where !navigation.IsCollection
                where navigation.TargetEntityType.IsOwned()
                select navigation;
            var properties = _entityType.GetProperties().Concat<IPropertyBase>(navigations).ToList();

            var target = new MergeTarget(_entityType);
            var source = new MergeSource(_dbContext, properties, builder, loader);
            var on = _on ?? new MergeOn(keys);
            var insert = _behavior.HasFlag(MergeBehavior.WhenNotMatchedByTargetThenInsert) ? _insert ?? new MergeInsert(properties) : default;
            var update = _behavior.HasFlag(MergeBehavior.WhenMatchedThenUpdate) ? _update ?? new MergeUpdate(properties) : default;
            var output = new MergeOutput(_dbContext, on.Properties.Union(_entityType.GetKeys().SelectMany(key => key.Properties).Distinct()));
            var merge = new Merge(_dbContext, target, source, on, _behavior, insert, update, output, _entityResolver, _principals, _dependents);

            var composite = new MergeComposite(_before.Append(merge).Concat(_after));
            return composite;
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

        public MergeBuilder On(MergeOn on)
        {
            _on = on;
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

        public MergeBuilder WithBehavior(MergeBehavior behavior, bool enable = true)
        {
            _behavior = enable ? _behavior | behavior : _behavior & ~behavior;
            return this;
        }

        public MergeBuilder WithSourceBuilder(IMergeSourceBuilder builder)
        {
            _sourceBuilder = builder;
            return this;
        }

        public MergeBuilder WithSourceLoader(IMergeSourceLoader loader)
        {
            _sourceLoader = loader;
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

        protected MergeBuilder Merge<TProperty>(LambdaExpression property, Action<MergeBuilder<TProperty>> build) where TProperty : class
        {
            var navigationBase = property.Body is MemberExpression body ? _entityType.FindNavigation(body.Member) ?? _entityType.FindSkipNavigation(body.Member) as INavigationBase : default;
            if (navigationBase == null)
                throw new ArgumentException("Expression body must describe a navigation property.");

            var builder = new MergeBuilder<TProperty>(_dbContext, _dbContext.Model.FindEntityType(typeof(TProperty)), new LazyEntityResolver(navigationBase, _entityResolver));

            build(builder);

            switch (navigationBase)
            {
                case ISkipNavigation skipNavigation:
                {
                    var joins =
                        new MergeBuilder(_dbContext, skipNavigation.JoinEntityType, new JoinEntityResolver(_dbContext, skipNavigation, _entityResolver))
                            .WithBehavior(MergeBehavior.WhenNotMatchedByTargetThenInsert)
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

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return ToMerge().ExecuteAsync(cancellationToken);
        }
    }

    public class MergeBuilder<T> : MergeBuilder where T : class
    {
        public MergeBuilder(DbContext dbContext, IEntityType entityType, IEntityResolver entityResolver)
            : base(dbContext, entityType, entityResolver)
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

        public new MergeBuilder<T> WithSourceBuilder(IMergeSourceBuilder builder)
        {
            return (MergeBuilder<T>) base.WithSourceBuilder(builder);
        }

        public new MergeBuilder<T> WithSourceLoader(IMergeSourceLoader loader)
        {
            return (MergeBuilder<T>) base.WithSourceLoader(loader);
        }

        public new MergeBuilder<T> On(MergeOn on)
        {
            return (MergeBuilder<T>) base.On(on);
        }

        public new MergeBuilder<T> WithBehavior(MergeBehavior behavior, bool enable = true)
        {
            return (MergeBuilder<T>) base.WithBehavior(behavior, enable);
        }

        public new MergeBuilder<T> Insert(MergeInsert insert)
        {
            return (MergeBuilder<T>) base.Insert(insert);
        }

        public new MergeBuilder<T> Update(MergeUpdate update)
        {
            return (MergeBuilder<T>) base.Update(update);
        }
    }
}