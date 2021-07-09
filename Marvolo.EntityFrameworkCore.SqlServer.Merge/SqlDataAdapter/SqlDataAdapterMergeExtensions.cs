using System;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlDataAdapter
{
    public static class SqlDataAdapterMergeExtensions
    {
        public static IServiceCollection AddSqlDataAdapterMergeStrategy(this IServiceCollection @this, Action<SqlDataAdapterMergeSourceLoaderOptions> options)
        {
            return @this.AddTransient<IMergeSourceLoader, SqlDataAdapterMergeSourceLoader>().Configure(options);
        }

        public static IServiceCollection AddSqlDataAdapterMergeStrategy(this IServiceCollection @this)
        {
            return @this.AddSqlDataAdapterMergeStrategy(_ => { });
        }

        public static MergeBuilder<T> UsingSqlDataAdapter<T>(this MergeBuilder<T> @this, SqlDataAdapterMergeSourceLoaderOptions options) where T : class
        {
            return @this.Using(new SqlDataAdapterMergeSourceLoader(Options.Create(options)));
        }

        public static MergeBuilder<T> UsingSqlDataAdapter<T>(this MergeBuilder<T> @this) where T : class
        {
            return @this.UsingSqlDataAdapter(new SqlDataAdapterMergeSourceLoaderOptions());
        }

        public static MergeBuilder<T> UsingSqlDataAdapter<T>(this MergeBuilder<T> @this, Action<SqlDataAdapterMergeSourceLoaderOptions> configure) where T : class
        {
            var options = new SqlDataAdapterMergeSourceLoaderOptions();
            configure(options);
            return @this.UsingSqlDataAdapter(options);
        }
    }
}