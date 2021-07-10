using System;
using Microsoft.Extensions.Options;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlBulkCopy
{
    public static class SqlBulkCopyMergeBuilderExtensions
    {
        public static MergeBuilder<T> UsingSqlBulkCopy<T>(this MergeBuilder<T> @this, Action<SqlBulkCopyMergeSourceLoaderOptions> configure = default) where T : class
        {
            var options = new SqlBulkCopyMergeSourceLoaderOptions();
            configure?.Invoke(options);
            return @this.Using(new SqlBulkCopyMergeSourceLoader(Options.Create(options)));
        }
    }
}