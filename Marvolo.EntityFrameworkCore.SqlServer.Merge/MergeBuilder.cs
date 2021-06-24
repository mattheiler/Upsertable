using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlBulkCopy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeBuilder<T> : IMergeBuilder where T : class
    {
        private readonly ICollection<IMergeBuilder> _dependents = new List<IMergeBuilder>();
        private readonly List<INavigation> _navigations = new List<INavigation>();
        private readonly ICollection<IMergeBuilder> _principals = new List<IMergeBuilder>();
        private MergeBehavior _behavior;
        private IMergeInsert _insert;
        private IMergeOn _on;
        private IMergeUpdate _update;
        private IMergeSourceLoadStrategy _loader;

        public MergeBuilder(MergeContext context)
        {
            Context = context;
        }

        public MergeContext Context { get; }

        public MergeBuilder<T> On(IMergeOn on)
        {
            _on = on;
            return this;
        }

        public MergeBuilder<T> Insert(IMergeInsert insert)
        {
            _insert = insert;
            return this;
        }

        public MergeBuilder<T> Update(IMergeUpdate update)
        {
            _update = update;
            return this;
        }

        public MergeBuilder<T> Behavior(MergeBehavior behavior, bool enable = true)
        {
            _behavior = enable ? _behavior | behavior : _behavior & ~behavior;
            return this;
        }

        public MergeBuilder<T> Include<TProperty>(Expression<Func<T, TProperty>> property, Action<MergeBuilder<TProperty>> build) where TProperty : class
        {
            var builder = new MergeBuilder<TProperty>(Context);

            build(builder);

            var navigation = Context.Db.Model.FindEntityType(typeof(T)).FindNavigation(((MemberExpression)property.Body).Member);
            if (navigation.IsDependentToPrincipal())
                _principals.Add(builder);
            else
                _dependents.Add(builder);

            _navigations.Add(navigation);

            return this;
        }

        public MergeBuilder<T> IncludeMany<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> property, Action<MergeBuilder<TProperty>> build) where TProperty : class
        {
            var builder = new MergeBuilder<TProperty>(Context);

            build(builder);

            var navigation = Context.Db.Model.FindEntityType(typeof(T)).FindNavigation(((MemberExpression)property.Body).Member);
            if (navigation.IsDependentToPrincipal())
                _principals.Add(builder);
            else
                _dependents.Add(builder);

            _navigations.Add(navigation);

            return this;
        }

        public MergeBuilder<T> Using(IMergeSourceLoadStrategy loader)
        {
            _loader = loader;
            return this;
        }

        public IMerge ToMerge()
        {
            var target = Context.Db.Model.FindEntityType(typeof(T));
            var loader = _loader ?? new SqlBulkCopyMergeSourceLoadStrategy(); // TODO Context.Db.GetService<IOptions<MergeOptions>>().Value.DefaultLoadStrategy;
            var source = new MergeSource(Context.Db, target, loader);
            var output = new MergeOutput(Context.Db, target, target.GetColumns().OfType<IProperty>().Where(property => property.IsPrimaryKey()));
            var on = _on ?? MergeOn.SelectPrimaryKeys(target);
            var insert = _insert ?? MergeInsert.SelectAll(target);
            var update = _update ?? MergeUpdate.SelectAll(target);

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

            var merge = new Merge(target, source, on, _behavior, insert, update, output, Context).WithNoTracking();

            return principals.Append(merge).Concat(dependents).ToComposite();
        }
    }
}