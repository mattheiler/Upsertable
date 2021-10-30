using System;
using Microsoft.Extensions.DependencyInjection;
using Upsertable.Abstractions;
using Upsertable.SqlServer.Infrastructure;

namespace Upsertable.SqlServer.SqlBulkCopy.Infrastructure.Extensions
{
    public static class SqlBulkCopyMergeSqlServerDbContextOptionsBuilderExtensions
    {
        public static MergeSqlServerDbContextOptionsBuilder UseSqlBulkCopy(this MergeSqlServerDbContextOptionsBuilder @this, Action<SqlBulkCopyDataTableLoaderOptions> configure = default)
        {
            return @this.SourceLoader(SqlBulkCopyMergeSourceLoaderFactory(configure));
        }

        public static MergeSqlServerDbContextOptionsBuilder UseSqlBulkCopy(this MergeSqlServerDbContextOptionsBuilder @this, ServiceLifetime lifetime, Action<SqlBulkCopyDataTableLoaderOptions> configure = default)
        {
            return @this.SourceLoader(SqlBulkCopyMergeSourceLoaderFactory(configure), lifetime);
        }

        private static Func<IServiceProvider, IDataTableLoader> SqlBulkCopyMergeSourceLoaderFactory(Action<SqlBulkCopyDataTableLoaderOptions> configure)
        {
            return provider =>
            {
                var options = new SqlBulkCopyDataTableLoaderOptions();
                configure?.Invoke(options);
                return new SqlBulkCopyDataTableLoader(options);
            };
        }
    }
}