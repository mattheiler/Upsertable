using System;

namespace Marvolo.EntityFramework.SqlMerge.SqlBulkCopy.Extensions
{
    public static class SqlBulkCopyMergeBuilderExtensions
    {
        public static MergeBuilder<T> UsingSqlBulkCopy<T>(this MergeBuilder<T> @this, Action<SqlBulkCopyMergeSourceLoaderOptions> configure = default) where T : class
        {
            var options = new SqlBulkCopyMergeSourceLoaderOptions();
            configure?.Invoke(options);
            return @this.WithSourceLoader(new SqlBulkCopyMergeSourceLoader(options));
        }
    }
}