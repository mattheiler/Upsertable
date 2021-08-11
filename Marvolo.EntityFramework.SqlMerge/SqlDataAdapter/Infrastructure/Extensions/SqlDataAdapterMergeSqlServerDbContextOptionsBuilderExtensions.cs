using System;
using Marvolo.EntityFramework.SqlMerge.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Marvolo.EntityFramework.SqlMerge.SqlDataAdapter.Infrastructure.Extensions
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