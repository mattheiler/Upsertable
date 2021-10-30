using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Upsertable.SqlServer.Infrastructure.Extensions;

namespace Upsertable.SqlServer.SqlBulkCopy.Infrastructure.Extensions
{
    public static class SqlBulkCopySqlServerDbContextOptionsBuilderExtensions
    {
        public static SqlServerDbContextOptionsBuilder UseMergeWithSqlBulkCopy(this SqlServerDbContextOptionsBuilder @this, Action<SqlBulkCopyDataTableLoaderOptions> configure = default)
        {
            return @this.UseMerge(merge => SqlBulkCopyMergeSqlServerDbContextOptionsBuilderExtensions.UseSqlBulkCopy(merge, configure));
        }

        public static SqlServerDbContextOptionsBuilder UseMergeWithSqlBulkCopy(this SqlServerDbContextOptionsBuilder @this, ServiceLifetime lifetime, Action<SqlBulkCopyDataTableLoaderOptions> configure = default)
        {
            return @this.UseMerge(merge => merge.UseSqlBulkCopy(lifetime, configure));
        }
    }
}