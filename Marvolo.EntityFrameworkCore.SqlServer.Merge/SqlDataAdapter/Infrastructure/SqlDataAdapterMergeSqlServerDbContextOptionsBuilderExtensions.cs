using System;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlDataAdapter.Infrastructure
{
    public static class SqlDataAdapterMergeSqlServerDbContextOptionsBuilderExtensions
    {
        public static MergeSqlServerDbContextOptionsBuilder UseSqlDataAdapter(this MergeSqlServerDbContextOptionsBuilder @this, Action<SqlDataAdapterMergeSourceLoaderOptions> configure = default)
        {
            return @this.SourceLoader(SqlDataAdapterMergeSourceLoaderFactory(configure));
        }

        public static MergeSqlServerDbContextOptionsBuilder UseSqlDataAdapter(this MergeSqlServerDbContextOptionsBuilder @this, ServiceLifetime lifetime, Action<SqlDataAdapterMergeSourceLoaderOptions> configure = default)
        {
            return @this.SourceLoader(SqlDataAdapterMergeSourceLoaderFactory(configure), lifetime);
        }

        private static Func<IServiceProvider, IMergeSourceLoader> SqlDataAdapterMergeSourceLoaderFactory(Action<SqlDataAdapterMergeSourceLoaderOptions> configure)
        {
            return provider =>
            {
                var options = new SqlDataAdapterMergeSourceLoaderOptions();
                configure?.Invoke(options);
                return new SqlDataAdapterMergeSourceLoader(options);
            };
        }
    }
}