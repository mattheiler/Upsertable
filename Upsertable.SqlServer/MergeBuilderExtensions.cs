using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Upsertable.Extensions;

namespace Upsertable.SqlServer;

public static class MergeBuilderExtensions
{
    public static MergeBuilder<T> Merge<T>(this DbContext @this, IEnumerable<T> entities) where T : class
    {
        return new MergeBuilder<T>(@this, entities.Distinct);
    }

    public static MergeBuilder<T> Insert<T>(this MergeBuilder<T> @this) where T : class
    {
        return @this.WithBehavior(MergeBehavior.Insert);
    }

    public static MergeBuilder<T> Insert<T, TProperty>(this MergeBuilder<T> @this, Expression<Func<T, TProperty>> insert) where T : class
    {
        return @this.WithBehavior(MergeBehavior.Insert).Insert(@this.EntityType.GetPropertiesAndNavigations(insert).ToList());
    }

    public static MergeBuilder<T> On<T, TProperty>(this MergeBuilder<T> @this, Expression<Func<T, TProperty>> on) where T : class
    {
        return @this.On(@this.EntityType.GetPropertiesAndNavigations(on).Cast<IProperty>().ToList());
    }

    public static MergeBuilder<T> Update<T>(this MergeBuilder<T> @this) where T : class
    {
        return @this.WithBehavior(MergeBehavior.Update);
    }

    public static MergeBuilder<T> Update<T, TProperty>(this MergeBuilder<T> @this, Expression<Func<T, TProperty>> update) where T : class
    {
        return @this.WithBehavior(MergeBehavior.Update).Update(@this.EntityType.GetPropertiesAndNavigations(update).ToList());
    }
}