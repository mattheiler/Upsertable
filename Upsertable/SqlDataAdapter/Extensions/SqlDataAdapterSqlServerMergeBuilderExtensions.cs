using System;

namespace Upsertable.SqlDataAdapter.Extensions;

public static class SqlDataAdapterSqlServerMergeBuilderExtensions
{
    public static MergeBuilder<T> UsingSqlDataAdapter<T>(this MergeBuilder<T> @this, Action<SqlDataAdapterDataLoaderOptions>? configure = default) where T : class
    {
        var options = new SqlDataAdapterDataLoaderOptions();
        configure?.Invoke(options);
        return @this.WithSourceLoader(new SqlDataAdapterDataLoader(options));
    }
}