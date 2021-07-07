using System;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlBulkCopy
{
    public static class SqlBulkCopyMergeExtensions
    {
        public static IServiceCollection AddSqlBulkCopyMergeStrategy(this IServiceCollection @this, Action<SqlBulkCopyMergeSourceLoadOptions> options)
        {
            return @this.AddTransient<IMergeSourceLoadStrategy, SqlBulkCopyMergeSourceLoadStrategy>().Configure(options);
        }

        public static IServiceCollection AddSqlBulkCopyMergeStrategy(this IServiceCollection @this)
        {
            return @this.AddSqlBulkCopyMergeStrategy(_ => { });
        }

        public static MergeBuilder<T> UsingSqlBulkCopy<T>(this MergeBuilder<T> @this, SqlBulkCopyMergeSourceLoadOptions options) where T : class
        {
            return @this.Using(new SqlBulkCopyMergeSourceLoadStrategy(Options.Create(options)));
        }

        public static MergeBuilder<T> UsingSqlBulkCopy<T>(this MergeBuilder<T> @this) where T : class
        {
            return @this.UsingSqlBulkCopy(new SqlBulkCopyMergeSourceLoadOptions());
        }

        public static MergeBuilder<T> UsingSqlBulkCopy<T>(this MergeBuilder<T> @this, Action<SqlBulkCopyMergeSourceLoadOptions> configure) where T : class
        {
            var options = new SqlBulkCopyMergeSourceLoadOptions();
            configure(options);
            return @this.UsingSqlBulkCopy(options);
        }
    }
}