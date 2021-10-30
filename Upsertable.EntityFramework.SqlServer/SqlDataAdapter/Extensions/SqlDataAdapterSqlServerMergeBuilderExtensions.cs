using System;

namespace Upsertable.EntityFramework.SqlServer.SqlDataAdapter.Extensions
{
    public static class SqlDataAdapterSqlServerMergeBuilderExtensions
    {
        public static SqlServerMergeBuilder<T> UsingSqlDataAdapter<T>(this SqlServerMergeBuilder<T> @this, Action<SqlDataAdapterDataTableLoaderOptions> configure = default) where T : class
        {
            var options = new SqlDataAdapterDataTableLoaderOptions();
            configure?.Invoke(options);
            return @this.WithSourceLoader(new SqlDataAdapterDataTableLoader(options));
        }
    }
}