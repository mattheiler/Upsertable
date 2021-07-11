using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeBuilder<T> : IMergeBuilder where T : class
    {
        private readonly DbContext _db;
        private readonly ICollection<IMergeBuilder> _dependents = new List<IMergeBuilder>();
        private readonly List<INavigation> _navigations = new List<INavigation>();
        private readonly ICollection<IMergeBuilder> _principals = new List<IMergeBuilder>();
        private MergeBehavior _behavior;
        private IEntityType _entityType;
        private MergeInsert _insert;
        private IMergeSourceLoader _loader;
        private MergeOn _on;
        private MergeUpdate _update;

        public MergeBuilder(DbContext db, MergeContext context)
        {
            _db = db;
            Context = context;
        }

        public MergeContext Context { get; }

        private IEntityType EntityType => _entityType ??= _db.Model.FindEntityType(typeof(T));

        public IMerge ToMerge()
        {
            var loader = _loader ?? _db.GetService<IMergeSourceLoader>();
            var target = new MergeTarget(EntityType);
            var source = new MergeSource(_db, EntityType, loader);
            var on = _on ?? new MergeOn(EntityType.GetProperties().Where(property => property.IsPrimaryKey()));
            var insert = _insert ?? new MergeInsert(EntityType.GetPropertiesAndOwnedNavigations());
            var update = _update ?? new MergeUpdate(EntityType.GetPropertiesAndOwnedNavigations());
            var output = new MergeOutput(_db, EntityType, EntityType.GetPropertiesAndOwnedNavigations().OfType<IProperty>().Where(property => property.IsPrimaryKey()));

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
                    Context.AddRange(type, (IEnumerable)value);
                else
                    Context.Add(type, value);
            }

            var principals = _principals.Select(principal => principal.ToMerge());
            var dependents = _dependents.Select(dependent => dependent.ToMerge());

            var merges = principals.Append(new Merge(_db, target, source, on, _behavior, insert, update, output)).Concat(dependents).ToList();

            return new MergeComposite(merges);
        }

        public MergeBuilder<T> Merge<TProperty>(MemberInfo property, Action<MergeBuilder<TProperty>> build) where TProperty : class
        {
            var navigation = EntityType.FindNavigation(property);
            if (navigation == null)
                throw new ArgumentException("Expression body must describe a navigation property.");

            var builder = new MergeBuilder<TProperty>(_db, Context);

            build(builder);

            if (navigation.IsDependentToPrincipal())
                _principals.Add(builder);
            else
                _dependents.Add(builder);

            _navigations.Add(navigation);

            return this;
        }

        public MergeBuilder<T> Merge<TProperty>(LambdaExpression property, Action<MergeBuilder<TProperty>> build) where TProperty : class
        {
            var body = property.Body as MemberExpression;
            if (body == null)
                throw new ArgumentException("Expression body must describe a navigation property.");

            return Merge(body.Member, build);
        }

        public MergeBuilder<T> Merge<TProperty>(Expression<Func<T, TProperty>> property, Action<MergeBuilder<TProperty>> build) where TProperty : class
        {
            return Merge((LambdaExpression) property, build);
        }

        public MergeBuilder<T> MergeMany<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> property, Action<MergeBuilder<TProperty>> build) where TProperty : class
        {
            return Merge(property, build);
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
            return On(new MergeOn(EntityType.GetPropertiesAndNavigations(on).Cast<IProperty>()));
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
            return Behavior(MergeBehavior.WhenNotMatchedByTargetThenInsert).Insert(new MergeInsert(EntityType.GetPropertiesAndOwnedNavigations()));
        }

        public MergeBuilder<T> Insert<TProperty>(Expression<Func<T, TProperty>> insert)
        {
            return Behavior(MergeBehavior.WhenNotMatchedByTargetThenInsert).Insert(new MergeInsert(EntityType.GetPropertiesAndNavigations(insert)));
        }

        public MergeBuilder<T> Update(MergeUpdate update)
        {
            _update = update;
            return this;
        }

        public MergeBuilder<T> Update()
        {
            return Behavior(MergeBehavior.WhenMatchedThenUpdate).Update(new MergeUpdate(EntityType.GetPropertiesAndOwnedNavigations()));
        }

        public MergeBuilder<T> Update<TProperty>(Expression<Func<T, TProperty>> update)
        {
            return Behavior(MergeBehavior.WhenMatchedThenUpdate).Update(new MergeUpdate(EntityType.GetPropertiesAndNavigations(update)));
        }

        public MergeBuilder<T> Delete()
        {
            return Behavior(MergeBehavior.WhenNotMatchedBySourceThenDelete);
        }

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var merge = ToMerge();
            return merge.ExecuteAsync(Context, cancellationToken);
        }
    }
}