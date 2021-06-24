using System;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlDataAdapter
{
    public static class SqlDataAdapterMergeBuilderExtensions
    {
        public static MergeBuilder<T> UsingSqlDataAdapter<T>(this MergeBuilder<T> @this, SqlDataAdapterMergeSourceLoadOptions options) where T : class
        {
            return @this.Using(new SqlDataAdapterMergeSourceLoadStrategy(options));
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