using System;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlDataAdapter.Infrastructure
{
    public static class SqlDataAdapterSqlServerDbContextOptionsBuilderExtensions
    {
        public static SqlServerDbContextOptionsBuilder UseMergeWithSqlDataAdapter(this SqlServerDbContextOptionsBuilder @this, Action<SqlDataAdapterMergeSourceLoaderOptions> configure = default)
        {
            return @this.UseMerge(merge => merge.UseSqlDataAdapter(configure));
        }

        public static SqlServerDbContextOptionsBuilder UseMergeWithSqlDataAdapter(this SqlServerDbContextOptionsBuilder @this, ServiceLifetime lifetime, Action<SqlDataAdapterMergeSourceLoaderOptions> configure = default)
        {
            return @this.UseMerge(merge => merge.UseSqlDataAdapter(lifetime, configure));
        }
    }
}