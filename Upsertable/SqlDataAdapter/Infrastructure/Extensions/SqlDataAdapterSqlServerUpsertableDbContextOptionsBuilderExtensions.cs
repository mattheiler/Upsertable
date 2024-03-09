using System;
using Upsertable.Infrastructure;

namespace Upsertable.SqlDataAdapter.Infrastructure.Extensions;

public static class SqlDataAdapterSqlServerUpsertableDbContextOptionsBuilderExtensions
{
    public static SqlServerUpsertableDbContextOptionsBuilder UseSqlDataAdapter(this SqlServerUpsertableDbContextOptionsBuilder @this, Action<SqlDataAdapterDataLoaderOptions>? configure = default)
    {
        return @this.SourceLoader(_ =>
        {
            var options = new SqlDataAdapterDataLoaderOptions();
            configure?.Invoke(options);
            return new SqlDataAdapterDataLoader(options);
        });
    }
}