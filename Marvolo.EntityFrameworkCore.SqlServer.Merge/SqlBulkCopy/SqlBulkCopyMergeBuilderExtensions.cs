using System;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlBulkCopy
{
    public static class SqlBulkCopyMergeBuilderExtensions
    {
        public static MergeBuilder<T> UsingSqlBulkCopy<T>(this MergeBuilder<T> @this, SqlBulkCopyMergeSourceLoadOptions options) where T : class
        {
            return @this.Using(new SqlBulkCopyMergeSourceLoadStrategy(options));
        }

        public static MergeBuilder<T> UsingSqlBulkCopy<T>(this MergeBuilder<T> @this) where T : class
        {
            return @this.UsingSqlBulkCopy(new SqlBulkCopyMergeSourceLoadOptions());
        }

        public static MergeBuilder<T> UsingSqlBulkCopy<T>(this MergeBuilder<T> @this, Action<SqlBulkCopyMergeSourceLoadOptions> configure) where T : class
        {
            var options = new SqlBulkCopyMergeSourceLoadOptions();
            configure(options);
            return @this.UsingSqlBulkCopy(options);
        }
    }
}