using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Upsertable.Abstractions;
using Upsertable.Data;

namespace Upsertable.SqlServer
{
    public class SqlServerMergeBuilder : IMergeBuilder
    {
        private readonly ICollection<IMerge> _after = new List<IMerge>();
        private readonly ICollection<IMerge> _before = new List<IMerge>();
        private readonly DbContext _dbContext;
        private readonly List<INavigation> _dependents = new();
        private readonly EntityProviderFunc _entityProviderFunc;
        private readonly IEntityType _entityType;
        private readonly List<INavigation> _principals = new();

        private MergeBehavior _behavior;
        private IDataTableLoader _dataTableLoader;
        private IDataTableFactory _dataTableResolver;
        private IReadOnlyCollection<IPropertyBase> _insert;

        private IReadOnlyCollection<IProperty> _on;
        private IReadOnlyCollection<IPropertyBase> _update;
        private bool _readonly;

        public SqlServerMergeBuilder(DbContext dbContext, IEntityType entityType, EntityProviderFunc entityProviderFunc)
        {
            _dbContext = dbContext;
            _entityType = entityType;
            _entityProviderFunc = entityProviderFunc;
        }

        // ReSharper disable once ConvertToAutoPropertyWhenPossible
        internal IEntityType EntityType => _entityType;

        public IMerge ToMerge()
        {
            var builder = _dataTableResolver ?? _dbContext.GetService<IDataTableFactory>();
            var loader = _dataTableLoader ?? _dbContext.GetService<IDataTableLoader>();

            var keys = _entityType.FindPrimaryKey().Properties;
            var navigations =
                from navigation in _entityType.GetNavigations()
                where !navigation.IsOnDependent
                where !navigation.IsCollection
                where navigation.TargetEntityType.IsOwned()
                select navigation;
            var properties = _entityType.GetProperties().Concat<IPropertyBase>(navigations).ToList();

            var source = new SqlServerMergeSource(_dbContext, properties, builder, loader);
            var on = _on ?? keys;
            var insert = _behavior.HasFlag(MergeBehavior.Insert) ? _insert ?? properties : default;
            var update = _behavior.HasFlag(MergeBehavior.Update) ? _update ?? properties : default;
            var output = new SqlServerMergeOutput(_dbContext, on.Union(_entityType.GetKeys().SelectMany(key => key.Properties).Distinct()));
            var merge = new SqlServerMerge(_dbContext, _entityType, source, output, _entityProviderFunc) { Behavior = _behavior, IsReadOnly = _readonly };

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

        public SqlServerMergeBuilder AsReadOnly(bool @readonly = true)
        {
            _readonly = @readonly;
            return this;
        }

        public SqlServerMergeBuilder MergeBefore(IMerge merge)
        {
            _after.Add(merge);
            return this;
        }

        public SqlServerMergeBuilder MergeAfter(IMerge merge)
        {
            _before.Add(merge);
            return this;
        }

        public SqlServerMergeBuilder On(IReadOnlyCollection<IProperty> on)
        {
            _on = on;
            return this;
        }

        public SqlServerMergeBuilder Insert(IReadOnlyCollection<IPropertyBase> insert)
        {
            _insert = insert;
            return this;
        }

        public SqlServerMergeBuilder Update(IReadOnlyCollection<IPropertyBase> update)
        {
            _update = update;
            return this;
        }

        public SqlServerMergeBuilder WithBehavior(MergeBehavior behavior, bool enable = true)
        {
            _behavior = enable ? _behavior | behavior : _behavior & ~behavior;
            return this;
        }

        public SqlServerMergeBuilder WithSourceBuilder(IDataTableFactory tableResolver)
        {
            _dataTableResolver = tableResolver;
            return this;
        }

        public SqlServerMergeBuilder WithSourceLoader(IDataTableLoader tableLoader)
        {
            _dataTableLoader = tableLoader;
            return this;
        }

        public SqlServerMergeBuilder WithPrincipal(INavigation navigation)
        {
            _principals.Add(navigation);
            return this;
        }

        public SqlServerMergeBuilder WithDependent(INavigation navigation)
        {
            _dependents.Add(navigation);
            return this;
        }

        protected SqlServerMergeBuilder Merge<TProperty>(LambdaExpression property, Action<SqlServerMergeBuilder<TProperty>> build) where TProperty : class
        {
            var navigationBase = property.Body is MemberExpression body ? _entityType.FindNavigation(body.Member) ?? _entityType.FindSkipNavigation(body.Member) as INavigationBase : default;
            if (navigationBase == null)
                throw new ArgumentException("Expression body must describe a navigation property.");

            var builder = new SqlServerMergeBuilder<TProperty>(_dbContext, _dbContext.Model.FindEntityType(typeof(TProperty)), EntityProvider.Lazy(navigationBase, _entityProviderFunc));

            build(builder);

            switch (navigationBase)
            {
                case ISkipNavigation skipNavigation:
                {
                    var joins =
                        new SqlServerMergeBuilder(_dbContext, skipNavigation.JoinEntityType, EntityProvider.Join(_dbContext, skipNavigation, _entityProviderFunc))
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

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return ToMerge().ExecuteAsync(cancellationToken);
        }
    }

    public class SqlServerMergeBuilder<T> : SqlServerMergeBuilder where T : class
    {
        public SqlServerMergeBuilder(DbContext dbContext, IEntityType entityType, EntityProviderFunc entityProviderFunc)
            : base(dbContext, entityType, entityProviderFunc)
        {
        }

        public SqlServerMergeBuilder<T> Merge<TProperty>(Expression<Func<T, TProperty>> property, Action<SqlServerMergeBuilder<TProperty>> build) where TProperty : class
        {
            return (SqlServerMergeBuilder<T>)base.Merge(property, build);
        }

        public SqlServerMergeBuilder<T> MergeMany<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> property, Action<SqlServerMergeBuilder<TProperty>> build) where TProperty : class
        {
            return (SqlServerMergeBuilder<T>)Merge(property, build);
        }

        public new SqlServerMergeBuilder<T> WithSourceBuilder(IDataTableFactory tableResolver)
        {
            return (SqlServerMergeBuilder<T>)base.WithSourceBuilder(tableResolver);
        }

        public new SqlServerMergeBuilder<T> WithSourceLoader(IDataTableLoader tableLoader)
        {
            return (SqlServerMergeBuilder<T>)base.WithSourceLoader(tableLoader);
        }

        public new SqlServerMergeBuilder<T> On(IReadOnlyCollection<IProperty> on)
        {
            return (SqlServerMergeBuilder<T>)base.On(on);
        }

        public new SqlServerMergeBuilder<T> WithBehavior(MergeBehavior behavior, bool enable = true)
        {
            return (SqlServerMergeBuilder<T>)base.WithBehavior(behavior, enable);
        }

        public new SqlServerMergeBuilder<T> Insert(IReadOnlyCollection<IPropertyBase> insert)
        {
            return (SqlServerMergeBuilder<T>)base.Insert(insert);
        }

        public new SqlServerMergeBuilder<T> Update(IReadOnlyCollection<IPropertyBase> update)
        {
            return (SqlServerMergeBuilder<T>)base.Update(update);
        }
    }
}