﻿using System;
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
        protected readonly DbContext Db;
        protected readonly ICollection<IMergeBuilder> Dependents = new List<IMergeBuilder>();
        protected readonly List<INavigationBase> Navigations = new();
        protected readonly ICollection<IMergeBuilder> Principals = new List<IMergeBuilder>();

        protected MergeBehavior Behavior;
        protected MergeInsert Insert;
        protected MergeOn On;
        protected IMergeSourceBuilder SourceBuilder;
        protected IMergeSourceLoader SourceLoader;
        protected MergeUpdate Update;

        public MergeBuilder(DbContext db, MergeContext context, IEntityType entityType)
        {
            Db = db;
            Context = context;
            EntityType = entityType;
        }

        internal MergeContext Context { get; }

        internal IEntityType EntityType { get; }

        public IMerge ToMerge()
        {
            var builder = SourceBuilder ?? Db.GetService<IMergeSourceBuilder>();
            var loader = SourceLoader ?? Db.GetService<IMergeSourceLoader>();
            var keys = EntityType.FindPrimaryKey().Properties;
            var navigations =
                from navigation in EntityType.GetNavigations()
                where !navigation.IsOnDependent
                where !navigation.IsCollection
                where navigation.TargetEntityType.IsOwned()
                select navigation;
            var properties = EntityType.GetProperties().Concat<IPropertyBase>(navigations).ToList();

            var target = new MergeTarget(EntityType);
            var source = new MergeSource(Db, properties, builder, loader);
            var on = On ?? new MergeOn(keys);
            var insert = Behavior.HasFlag(MergeBehavior.WhenNotMatchedByTargetThenInsert) ? Insert ?? new MergeInsert(properties) : null;
            var update = Behavior.HasFlag(MergeBehavior.WhenMatchedThenUpdate) ? Update ?? new MergeUpdate(properties) : null;
            var output = new MergeOutput(Db, on.Properties.Union(EntityType.GetKeys().SelectMany(key => key.Properties).Distinct()));

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
            foreach (var navigationBase in Navigations)
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

            var principals = Principals.Select(principal => principal.ToMerge());
            var dependents = Dependents.Select(dependent => dependent.ToMerge());
            var merge = new Merge(Db, target, source, on, Behavior, insert, update, output);

            return new MergeComposite(principals.Append(merge).Concat(dependents));
        }

        public MergeBuilder Using(IMergeSourceBuilder builder)
        {
            SourceBuilder = builder;
            return this;
        }

        public MergeBuilder Using(IMergeSourceLoader loader)
        {
            SourceLoader = loader;
            return this;
        }

        public MergeBuilder WithOn(MergeOn on)
        {
            On = on;
            return this;
        }

        public MergeBuilder WithBehavior(MergeBehavior behavior, bool enable = true)
        {
            Behavior = enable ? Behavior | behavior : Behavior & ~behavior;
            return this;
        }

        public MergeBuilder WithInserts(MergeInsert insert)
        {
            Insert = insert;
            return this;
        }

        public MergeBuilder WithUpdates(MergeUpdate update)
        {
            Update = update;
            return this;
        }

        protected MergeBuilder Merge<TProperty>(LambdaExpression property, Action<MergeBuilder<TProperty>> build) where TProperty : class
        {
            var navigationBase = property.Body is MemberExpression body ? EntityType.FindNavigation(body.Member) ?? EntityType.FindSkipNavigation(body.Member) as INavigationBase : default;
            if (navigationBase == null)
                throw new ArgumentException("Expression body must describe a navigation property.");

            var builder = new MergeBuilder<TProperty>(Db, Context);

            build(builder);

            switch (navigationBase)
            {
                case ISkipNavigation skipNavigation:
                {
                    var skip = new MergeBuilder(Db, Context, skipNavigation.JoinEntityType).WithBehavior(MergeBehavior.WhenNotMatchedByTargetThenInsert);

                    if (skipNavigation.IsOnDependent)
                        builder.Dependents.Add(skip);
                    else
                        Dependents.Add(skip);

                    Principals.Add(builder);
                    break;
                }
                case INavigation navigation:
                {
                    if (navigation.IsOnDependent)
                        Principals.Add(builder);
                    else
                        Dependents.Add(builder);
                    break;
                }
                default:
                    throw new NotSupportedException("Unknown navigation type.");
            }

            Navigations.Add(navigationBase);

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
            return (MergeBuilder<T>) base.WithOn(on);
        }

        public new MergeBuilder<T> WithBehavior(MergeBehavior behavior, bool enable = true)
        {
            return (MergeBuilder<T>) base.WithBehavior(behavior, enable);
        }

        public new MergeBuilder<T> WithInserts(MergeInsert insert)
        {
            return (MergeBuilder<T>) base.WithInserts(insert);
        }

        public new MergeBuilder<T> WithUpdates(MergeUpdate update)
        {
            return (MergeBuilder<T>) base.WithUpdates(update);
        }
    }
}