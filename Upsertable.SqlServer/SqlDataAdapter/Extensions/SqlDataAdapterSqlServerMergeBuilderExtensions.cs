using System;

namespace Upsertable.SqlServer.SqlDataAdapter.Extensions;

public static class SqlDataAdapterSqlServerMergeBuilderExtensions
{
    public static SqlServerMergeBuilder<T> UsingSqlDataAdapter<T>(this SqlServerMergeBuilder<T> @this, Action<SqlDataAdapterDataLoaderOptions> configure = default) where T : class
    {
        var options = new SqlDataAdapterDataLoaderOptions();
        configure?.Invoke(options);
        return @this.WithSourceLoader(new SqlDataAdapterDataLoader(options));
    }
}