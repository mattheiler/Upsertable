using System;
using Upsertable.SqlServer.Infrastructure;

namespace Upsertable.SqlServer.SqlBulkCopy.Infrastructure.Extensions;

public static class SqlBulkCopySqlServerUpsertableDbContextOptionsBuilderExtensions
{
    public static SqlServerUpsertableDbContextOptionsBuilder UseSqlBulkCopy(this SqlServerUpsertableDbContextOptionsBuilder @this, Action<SqlBulkCopyDataTableLoaderOptions> configure = default)
    {
        return @this.SourceLoader(_ =>
        {
            var options = new SqlBulkCopyDataTableLoaderOptions();
            configure?.Invoke(options);
            return new SqlBulkCopyDataLoader(options);
        });
    }
}