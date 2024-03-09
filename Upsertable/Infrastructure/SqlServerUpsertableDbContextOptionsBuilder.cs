using System;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Upsertable.Infrastructure;

public class SqlServerUpsertableDbContextOptionsBuilder(SqlServerDbContextOptionsBuilder builder) : ISqlServerUpsertableDbContextOptionsInfrastructure
{
    SqlServerDbContextOptionsBuilder ISqlServerUpsertableDbContextOptionsInfrastructure.OptionsBuilder => builder;

    public SqlServerUpsertableDbContextOptionsBuilder SourceLoader(Func<IServiceProvider, IDataLoader> factory)
    {
        return WithOption(e => e.WithSourceLoader(factory));
    }

    public SqlServerUpsertableDbContextOptionsBuilder DataResolver(Func<IServiceProvider, IDataResolver> factory)
    {
        return WithOption(e => e.WithDataResolver(factory));
    }

    private SqlServerUpsertableDbContextOptionsBuilder WithOption(Func<SqlServerUpsertableDbContextOptionsExtension, SqlServerUpsertableDbContextOptionsExtension> configure)
    {
        var relational = ((IRelationalDbContextOptionsBuilderInfrastructure)builder).OptionsBuilder;
        var extension = relational.Options.FindExtension<SqlServerUpsertableDbContextOptionsExtension>() ?? new SqlServerUpsertableDbContextOptionsExtension();

        ((IDbContextOptionsBuilderInfrastructure)relational).AddOrUpdateExtension(configure(extension));

        return this;
    }
}