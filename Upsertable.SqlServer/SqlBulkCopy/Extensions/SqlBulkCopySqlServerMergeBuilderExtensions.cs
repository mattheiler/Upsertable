using System;

namespace Upsertable.SqlServer.SqlBulkCopy.Extensions
{
    public static class SqlBulkCopySqlServerMergeBuilderExtensions
    {
        public static SqlServerMergeBuilder<T> UsingSqlBulkCopy<T>(this SqlServerMergeBuilder<T> @this, Action<SqlBulkCopyDataTableLoaderOptions> configure = default) where T : class
        {
            var options = new SqlBulkCopyDataTableLoaderOptions();
            configure?.Invoke(options);
            return @this.WithSourceLoader(new SqlBulkCopyDataTableLoader(options));
        }
    }
}