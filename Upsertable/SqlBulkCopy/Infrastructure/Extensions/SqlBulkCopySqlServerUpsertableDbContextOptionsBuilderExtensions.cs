using System;
using Upsertable.Infrastructure;

namespace Upsertable.SqlBulkCopy.Infrastructure.Extensions;

public static class SqlBulkCopySqlServerUpsertableDbContextOptionsBuilderExtensions
{
    public static SqlServerUpsertableDbContextOptionsBuilder UseSqlBulkCopy(this SqlServerUpsertableDbContextOptionsBuilder @this, Action<SqlBulkCopyDataLoaderOptions>? configure = default)
    {
        return @this.SourceLoader(_ =>
        {
            var options = new SqlBulkCopyDataLoaderOptions();
            configure?.Invoke(options);
            return new SqlBulkCopyDataLoader(options);
        });
    }
}