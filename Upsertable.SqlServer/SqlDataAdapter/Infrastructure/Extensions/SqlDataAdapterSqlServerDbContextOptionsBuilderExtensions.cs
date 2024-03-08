using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Upsertable.SqlServer.Infrastructure.Extensions;

namespace Upsertable.SqlServer.SqlDataAdapter.Infrastructure.Extensions;

public static class SqlDataAdapterSqlServerDbContextOptionsBuilderExtensions
{
    public static SqlServerDbContextOptionsBuilder UseMergeWithSqlDataAdapter(this SqlServerDbContextOptionsBuilder @this, Action<SqlDataAdapterDataTableLoaderOptions> configure = default)
    {
        return @this.UseUpsertable(merge => merge.UseSqlDataAdapter(configure));
    }
}