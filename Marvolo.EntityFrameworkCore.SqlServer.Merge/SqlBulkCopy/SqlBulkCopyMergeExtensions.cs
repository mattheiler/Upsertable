using System;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlBulkCopy
{
    public static class SqlBulkCopyMergeExtensions
    {
        public static IServiceCollection AddSqlBulkCopyMergeStrategy(this IServiceCollection @this, Action<SqlBulkCopyMergeSourceLoaderOptions> options)
        {
            return @this.AddTransient<IMergeSourceLoader, SqlBulkCopyMergeSourceLoader>().Configure(options);
        }

        public static IServiceCollection AddSqlBulkCopyMergeStrategy(this IServiceCollection @this)
        {
            return @this.AddSqlBulkCopyMergeStrategy(_ => { });
        }

        public static MergeBuilder<T> UsingSqlBulkCopy<T>(this MergeBuilder<T> @this, SqlBulkCopyMergeSourceLoaderOptions options) where T : class
        {
            return @this.Using(new SqlBulkCopyMergeSourceLoader(Options.Create(options)));
        }

        public static MergeBuilder<T> UsingSqlBulkCopy<T>(this MergeBuilder<T> @this) where T : class
        {
            return @this.UsingSqlBulkCopy(new SqlBulkCopyMergeSourceLoaderOptions());
        }

        public static MergeBuilder<T> UsingSqlBulkCopy<T>(this MergeBuilder<T> @this, Action<SqlBulkCopyMergeSourceLoaderOptions> configure) where T : class
        {
            var options = new SqlBulkCopyMergeSourceLoaderOptions();
            configure(options);
            return @this.UsingSqlBulkCopy(options);
        }
    }
}