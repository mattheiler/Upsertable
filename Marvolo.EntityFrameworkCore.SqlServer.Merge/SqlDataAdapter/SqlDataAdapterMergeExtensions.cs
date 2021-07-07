using System;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlDataAdapter
{
    public static class SqlDataAdapterMergeExtensions
    {
        public static IServiceCollection AddSqlDataAdapterMergeStrategy(this IServiceCollection @this, Action<SqlDataAdapterMergeSourceLoadOptions> options)
        {
            return @this.AddTransient<IMergeSourceLoadStrategy, SqlDataAdapterMergeSourceLoadStrategy>().Configure(options);
        }

        public static IServiceCollection AddSqlDataAdapterMergeStrategy(this IServiceCollection @this)
        {
            return @this.AddSqlDataAdapterMergeStrategy(_ => { });
        }

        public static MergeBuilder<T> UsingSqlDataAdapter<T>(this MergeBuilder<T> @this, SqlDataAdapterMergeSourceLoadOptions options) where T : class
        {
            return @this.Using(new SqlDataAdapterMergeSourceLoadStrategy(Options.Create(options)));
        }

        public static MergeBuilder<T> UsingSqlDataAdapter<T>(this MergeBuilder<T> @this) where T : class
        {
            return @this.UsingSqlDataAdapter(new SqlDataAdapterMergeSourceLoadOptions());
        }

        public static MergeBuilder<T> UsingSqlDataAdapter<T>(this MergeBuilder<T> @this, Action<SqlDataAdapterMergeSourceLoadOptions> configure) where T : class
        {
            var options = new SqlDataAdapterMergeSourceLoadOptions();
            configure(options);
            return @this.UsingSqlDataAdapter(options);
        }
    }
}