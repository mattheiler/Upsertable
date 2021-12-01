using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Upsertable.SqlServer.Infrastructure.Extensions;

namespace Upsertable.SqlServer.SqlBulkCopy.Infrastructure.Extensions
{
    public static class SqlBulkCopySqlServerDbContextOptionsBuilderExtensions
    {
        public static SqlServerDbContextOptionsBuilder UseMergeWithSqlBulkCopy(this SqlServerDbContextOptionsBuilder @this, Action<SqlBulkCopyDataTableLoaderOptions> configure = default)
        {
            return @this.UseUpsertable(merge => merge.UseSqlBulkCopy(configure));
        }
    }
}