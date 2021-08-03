using System;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlBulkCopy.Infrastructure.Extensions
{
    public static class SqlBulkCopySqlServerDbContextOptionsBuilderExtensions
    {
        public static SqlServerDbContextOptionsBuilder UseMergeWithSqlBulkCopy(this SqlServerDbContextOptionsBuilder @this, Action<SqlBulkCopyMergeSourceLoaderOptions> configure = default)
        {
            return @this.UseMerge(merge => merge.UseSqlBulkCopy(configure));
        }

        public static SqlServerDbContextOptionsBuilder UseMergeWithSqlBulkCopy(this SqlServerDbContextOptionsBuilder @this, ServiceLifetime lifetime, Action<SqlBulkCopyMergeSourceLoaderOptions> configure = default)
        {
            return @this.UseMerge(merge => merge.UseSqlBulkCopy(lifetime, configure));
        }
    }
}