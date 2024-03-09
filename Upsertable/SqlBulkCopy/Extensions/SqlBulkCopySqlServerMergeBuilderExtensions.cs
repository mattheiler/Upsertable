using System;

namespace Upsertable.SqlBulkCopy.Extensions;

public static class SqlBulkCopySqlServerMergeBuilderExtensions
{
    public static MergeBuilder<T> UsingSqlBulkCopy<T>(this MergeBuilder<T> @this, Action<SqlBulkCopyDataLoaderOptions>? configure = default) where T : class
    {
        var options = new SqlBulkCopyDataLoaderOptions();
        configure?.Invoke(options);
        return @this.WithSourceLoader(new SqlBulkCopyDataLoader(options));
    }
}