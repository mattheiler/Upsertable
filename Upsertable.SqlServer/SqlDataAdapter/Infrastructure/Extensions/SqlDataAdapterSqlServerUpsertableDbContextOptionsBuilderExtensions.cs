using System;
using Upsertable.SqlServer.Infrastructure;

namespace Upsertable.SqlServer.SqlDataAdapter.Infrastructure.Extensions;

public static class SqlDataAdapterSqlServerUpsertableDbContextOptionsBuilderExtensions
{
    public static SqlServerUpsertableDbContextOptionsBuilder UseSqlDataAdapter(this SqlServerUpsertableDbContextOptionsBuilder @this, Action<SqlDataAdapterDataTableLoaderOptions> configure = default)
    {
        return @this.SourceLoader(_ =>
        {
            var options = new SqlDataAdapterDataTableLoaderOptions();
            configure?.Invoke(options);
            return new SqlDataAdapterDataTableLoader(options);
        });
    }
}