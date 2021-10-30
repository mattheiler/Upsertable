using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Upsertable.SqlServer.Infrastructure.Extensions;

namespace Upsertable.SqlServer.SqlDataAdapter.Infrastructure.Extensions
{
    public static class SqlDataAdapterSqlServerDbContextOptionsBuilderExtensions
    {
        public static SqlServerDbContextOptionsBuilder UseMergeWithSqlDataAdapter(this SqlServerDbContextOptionsBuilder @this, Action<SqlDataAdapterDataTableLoaderOptions> configure = default)
        {
            return @this.UseMerge(merge => SqlDataAdapterMergeSqlServerDbContextOptionsBuilderExtensions.UseSqlDataAdapter(merge, configure));
        }

        public static SqlServerDbContextOptionsBuilder UseMergeWithSqlDataAdapter(this SqlServerDbContextOptionsBuilder @this, ServiceLifetime lifetime, Action<SqlDataAdapterDataTableLoaderOptions> configure = default)
        {
            return @this.UseMerge(merge => merge.UseSqlDataAdapter(lifetime, configure));
        }
    }
}