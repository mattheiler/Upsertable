using System;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Upsertable.SqlServer.Infrastructure.Extensions;

public static class UpsertableDbContextOptionsBuilderExtensions
{
    public static SqlServerDbContextOptionsBuilder UseUpsertable(this SqlServerDbContextOptionsBuilder @this, Func<IServiceProvider, IDataLoader> loader)
    {
        return @this.UseUpsertable(merge => merge.SourceLoader(loader));
    }

    public static SqlServerDbContextOptionsBuilder UseUpsertable(this SqlServerDbContextOptionsBuilder @this, Action<SqlServerUpsertableDbContextOptionsBuilder>? configure = default)
    {
        var builder = ((IRelationalDbContextOptionsBuilderInfrastructure)@this).OptionsBuilder;
        var extension = builder.Options.FindExtension<SqlServerUpsertableDbContextOptionsExtension>() ?? new SqlServerUpsertableDbContextOptionsExtension();

        ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);

        configure?.Invoke(new SqlServerUpsertableDbContextOptionsBuilder(@this));

        return @this;
    }
}