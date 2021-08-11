using System;
using Marvolo.EntityFramework.SqlMerge.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Marvolo.EntityFramework.SqlMerge.SqlDataAdapter.Infrastructure.Extensions
{
    public static class SqlDataAdapterSqlServerDbContextOptionsBuilderExtensions
    {
        public static SqlServerDbContextOptionsBuilder UseMergeWithSqlDataAdapter(this SqlServerDbContextOptionsBuilder @this, Action<SqlDataAdapterMergeSourceLoaderOptions> configure = default)
        {
            return @this.UseMerge(merge => SqlDataAdapterMergeSqlServerDbContextOptionsBuilderExtensions.UseSqlDataAdapter(merge, configure));
        }

        public static SqlServerDbContextOptionsBuilder UseMergeWithSqlDataAdapter(this SqlServerDbContextOptionsBuilder @this, ServiceLifetime lifetime, Action<SqlDataAdapterMergeSourceLoaderOptions> configure = default)
        {
            return @this.UseMerge(merge => merge.UseSqlDataAdapter(lifetime, configure));
        }
    }
}