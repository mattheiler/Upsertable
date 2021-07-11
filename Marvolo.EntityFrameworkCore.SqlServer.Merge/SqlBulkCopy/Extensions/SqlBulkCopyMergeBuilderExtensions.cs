using System;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlBulkCopy.Extensions
{
    public static class SqlBulkCopyMergeBuilderExtensions
    {
        public static MergeBuilder<T> UsingSqlBulkCopy<T>(this MergeBuilder<T> @this, Action<SqlBulkCopyMergeSourceLoaderOptions> configure = default) where T : class
        {
            var options = new SqlBulkCopyMergeSourceLoaderOptions();
            configure?.Invoke(options);
            return @this.Using(new SqlBulkCopyMergeSourceLoader(options));
        }
    }
}