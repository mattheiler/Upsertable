using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public static class MergeBuilderExtensions
    {
        public static MergeBuilder<T> Merge<T>(this DbContext @this, IEnumerable<T> entities)
            where T : class
        {
            var context = new MergeContext(@this);
            context.AddRange(entities);
            return new MergeBuilder<T>(context);
        }

        public static MergeBuilder<T> On<T, TProperty>(this MergeBuilder<T> @this, Expression<Func<T, TProperty>> on) where T : class
        {
            return @this.On(new MergeOn(@this.Context.Db.Model.FindEntityType(typeof(T)).GetColumns(on).Cast<IProperty>()));
        }

        public static MergeBuilder<T> Insert<T>(this MergeBuilder<T> @this) where T : class
        {
            return @this.Behavior(MergeBehavior.WhenNotMatchedByTargetThenInsert).Insert(new MergeInsert(@this.Context.Db.Model.FindEntityType(typeof(T)).GetColumns()));
        }

        public static MergeBuilder<T> Insert<T, TProperty>(this MergeBuilder<T> @this, Expression<Func<T, TProperty>> insert) where T : class
        {
            return @this.Behavior(MergeBehavior.WhenNotMatchedByTargetThenInsert).Insert(new MergeInsert(@this.Context.Db.Model.FindEntityType(typeof(T)).GetColumns(insert)));
        }

        public static MergeBuilder<T> Update<T>(this MergeBuilder<T> @this) where T : class
        {
            return @this.Behavior(MergeBehavior.WhenMatchedThenUpdate).Update(new MergeUpdate(@this.Context.Db.Model.FindEntityType(typeof(T)).GetColumns()));
        }

        public static MergeBuilder<T> Update<T, TProperty>(this MergeBuilder<T> @this, Expression<Func<T, TProperty>> update) where T : class
        {
            return @this.Behavior(MergeBehavior.WhenMatchedThenUpdate).Update(new MergeUpdate(@this.Context.Db.Model.FindEntityType(typeof(T)).GetColumns(update)));
        }

        public static MergeBuilder<T> Delete<T>(this MergeBuilder<T> @this) where T : class
        {
            return @this.Behavior(MergeBehavior.WhenNotMatchedBySourceThenDelete);
        }

        public static Task ExecuteAsync<T>(this MergeBuilder<T> @this, CancellationToken cancellationToken = default) where T : class
        {
            return @this.ToMerge().ExecuteAsync(cancellationToken);
        }
    }
}