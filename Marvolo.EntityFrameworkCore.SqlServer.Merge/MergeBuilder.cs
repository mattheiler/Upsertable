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

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeBuilder<T> : IMergeBuilder where T : class
    {
        private readonly List<INavigation> _navigations = new List<INavigation>();
        private readonly ICollection<IMergeBuilder> _dependents = new List<IMergeBuilder>();
        private readonly ICollection<IMergeBuilder> _principals = new List<IMergeBuilder>();
        private IEntityType _entityType;
        private IMergeSourceLoader _loader;
        private MergeOn _on;
        private MergeBehavior _behavior;
        private MergeInsert _insert;
        private MergeUpdate _update;

        public MergeBuilder(MergeContext context)
        {
            Context = context;
        }

        public MergeContext Context { get; }

        private IEntityType EntityType => _entityType ??= Context.Db.Model.FindEntityType(typeof(T));

        public IMerge ToMerge()
        {
            var loader = _loader ?? Context.Db.GetService<IMergeSourceLoader>();
            var target = new MergeTarget(EntityType);
            var source = new MergeSource(Context.Db, EntityType, loader);
            var output = new MergeOutput(Context.Db, EntityType, EntityType.GetColumns().OfType<IProperty>().Where(property => property.IsPrimaryKey()));
            var on = _on ?? new MergeOn(EntityType.GetProperties().Where(property => property.IsPrimaryKey()));
            var insert = _insert ?? new MergeInsert(EntityType.GetColumns());
            var update = _update ?? new MergeUpdate(EntityType.GetColumns());

            foreach (var entity in Context.Get(typeof(T)))
            foreach (var navigation in _navigations)
            {
                var type =
                    navigation.IsDependentToPrincipal()
                        ? navigation.ForeignKey.PrincipalKey.DeclaringEntityType.ClrType
                        : navigation.ForeignKey.DeclaringEntityType.ClrType;

                var value = navigation.GetGetter().GetClrValue(entity);
                if (value == null)
                    continue;

                if (navigation.IsCollection())
                    Context.AddRange(type, (IEnumerable) value);
                else
                    Context.Add(type, value);
            }

            var principals = _principals.Select(principal => principal.ToMerge()).ToList();
            var dependents = _dependents.Select(dependent => dependent.ToMerge()).ToList();

            var merge = new Merge(target, source, on, _behavior, insert, update, output, Context);

            return new MergeComposite(principals.Append(merge).Concat(dependents));
        }

        public MergeBuilder<T> Include<TProperty>(Expression<Func<T, TProperty>> property, Action<MergeBuilder<TProperty>> build) where TProperty : class
        {
            var member = property.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException("Expression body must describe a navigation property.");

            var navigation = EntityType.FindNavigation(member.Member);
            if (navigation == null)
                throw new ArgumentException("Expression body must describe a navigation property.");

            _navigations.Add(navigation);

            var builder = new MergeBuilder<TProperty>(Context);

            build(builder);

            if (navigation.IsDependentToPrincipal())
                _principals.Add(builder);
            else
                _dependents.Add(builder);

            return this;
        }

        public MergeBuilder<T> IncludeMany<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> property, Action<MergeBuilder<TProperty>> build) where TProperty : class
        {
            var member = property.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException("Expression body must describe a navigation property.");

            var navigation = EntityType.FindNavigation(member.Member);
            if (navigation == null)
                throw new ArgumentException("Expression body must describe a navigation property.");

            _navigations.Add(navigation);

            var builder = new MergeBuilder<TProperty>(Context);

            build(builder);

            if (navigation.IsDependentToPrincipal())
                _principals.Add(builder);
            else
                _dependents.Add(builder);

            return this;
        }

        public MergeBuilder<T> Using(IMergeSourceLoader loader)
        {
            _loader = loader;
            return this;
        }

        public MergeBuilder<T> On(MergeOn on)
        {
            _on = on;
            return this;
        }

        public MergeBuilder<T> On<TProperty>(Expression<Func<T, TProperty>> on)
        {
            return On(new MergeOn(Context.Db.Model.FindEntityType(typeof(T)).GetColumns(on).Cast<IProperty>()));
        }

        public MergeBuilder<T> Behavior(MergeBehavior behavior, bool enable = true)
        {
            _behavior = enable ? _behavior | behavior : _behavior & ~behavior;
            return this;
        }

        public MergeBuilder<T> Insert(MergeInsert insert)
        {
            _insert = insert;
            return this;
        }

        public MergeBuilder<T> Insert()
        {
            return Behavior(MergeBehavior.WhenNotMatchedByTargetThenInsert).Insert(new MergeInsert(Context.Db.Model.FindEntityType(typeof(T)).GetColumns()));
        }

        public MergeBuilder<T> Insert<TProperty>(Expression<Func<T, TProperty>> insert)
        {
            return Behavior(MergeBehavior.WhenNotMatchedByTargetThenInsert).Insert(new MergeInsert(Context.Db.Model.FindEntityType(typeof(T)).GetColumns(insert)));
        }

        public MergeBuilder<T> Update(MergeUpdate update)
        {
            _update = update;
            return this;
        }

        public MergeBuilder<T> Update()
        {
            return Behavior(MergeBehavior.WhenMatchedThenUpdate).Update(new MergeUpdate(Context.Db.Model.FindEntityType(typeof(T)).GetColumns()));
        }

        public MergeBuilder<T> Update<TProperty>(Expression<Func<T, TProperty>> update)
        {
            return Behavior(MergeBehavior.WhenMatchedThenUpdate).Update(new MergeUpdate(Context.Db.Model.FindEntityType(typeof(T)).GetColumns(update)));
        }

        public MergeBuilder<T> Delete()
        {
            return Behavior(MergeBehavior.WhenNotMatchedBySourceThenDelete);
        }

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return ToMerge().ExecuteAsync(cancellationToken);
        }
    }
}