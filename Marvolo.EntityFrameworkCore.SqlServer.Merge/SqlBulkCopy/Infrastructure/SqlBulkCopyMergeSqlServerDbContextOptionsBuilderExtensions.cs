using System;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlBulkCopy.Infrastructure
{
    public static class SqlBulkCopyMergeSqlServerDbContextOptionsBuilderExtensions
    {
        public static MergeSqlServerDbContextOptionsBuilder UseSqlBulkCopy(this MergeSqlServerDbContextOptionsBuilder @this, Action<SqlBulkCopyMergeSourceLoaderOptions> configure = default)
        {
            return @this.SourceLoader(SqlBulkCopyMergeSourceLoaderFactory(configure));
        }

        public static MergeSqlServerDbContextOptionsBuilder UseSqlBulkCopy(this MergeSqlServerDbContextOptionsBuilder @this, ServiceLifetime lifetime, Action<SqlBulkCopyMergeSourceLoaderOptions> configure = default)
        {
            return @this.SourceLoader(SqlBulkCopyMergeSourceLoaderFactory(configure), lifetime);
        }

        private static Func<IServiceProvider, IMergeSourceLoader> SqlBulkCopyMergeSourceLoaderFactory(Action<SqlBulkCopyMergeSourceLoaderOptions> configure)
        {
            return provider =>
            {
                var options = new SqlBulkCopyMergeSourceLoaderOptions();
                configure?.Invoke(options);
                return new SqlBulkCopyMergeSourceLoader(Options.Create(options));
            };
        }
    }
}