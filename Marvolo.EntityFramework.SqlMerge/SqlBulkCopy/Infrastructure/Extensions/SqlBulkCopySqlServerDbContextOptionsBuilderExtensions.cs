using System;
using Marvolo.EntityFramework.SqlMerge.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Marvolo.EntityFramework.SqlMerge.SqlBulkCopy.Infrastructure.Extensions
{
    public static class SqlBulkCopySqlServerDbContextOptionsBuilderExtensions
    {
        public static SqlServerDbContextOptionsBuilder UseMergeWithSqlBulkCopy(this SqlServerDbContextOptionsBuilder @this, Action<SqlBulkCopyMergeSourceLoaderOptions> configure = default)
        {
            return @this.UseMerge(merge => SqlBulkCopyMergeSqlServerDbContextOptionsBuilderExtensions.UseSqlBulkCopy(merge, configure));
        }

        public static SqlServerDbContextOptionsBuilder UseMergeWithSqlBulkCopy(this SqlServerDbContextOptionsBuilder @this, ServiceLifetime lifetime, Action<SqlBulkCopyMergeSourceLoaderOptions> configure = default)
        {
            return @this.UseMerge(merge => merge.UseSqlBulkCopy(lifetime, configure));
        }
    }
}