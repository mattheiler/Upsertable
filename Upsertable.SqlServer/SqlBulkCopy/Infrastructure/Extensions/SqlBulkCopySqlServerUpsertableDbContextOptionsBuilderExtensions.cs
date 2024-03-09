using System;
using Upsertable.SqlServer.Infrastructure;

namespace Upsertable.SqlServer.SqlBulkCopy.Infrastructure.Extensions;

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