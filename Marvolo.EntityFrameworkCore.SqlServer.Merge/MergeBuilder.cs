using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeBuilder<T> : IMergeBuilder where T : class
    {
        private readonly ICollection<IMergeBuilder> _dependents = new List<IMergeBuilder>();
        private readonly List<INavigation> _navigations = new List<INavigation>();
        private readonly ICollection<IMergeBuilder> _principals = new List<IMergeBuilder>();
        private MergeBehavior _behavior;
        private IEntityType _entityType;
        private IMergeInsert _insert;
        private IMergeSourceLoader _loader;
        private IMergeOn _on;
        private IMergeUpdate _update;

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
    }
}