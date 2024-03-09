using System;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Upsertable.Infrastructure;

public class SqlServerUpsertableDbContextOptionsBuilder : ISqlServerUpsertableDbContextOptionsInfrastructure
{
    private readonly SqlServerDbContextOptionsBuilder _builder;

    public SqlServerUpsertableDbContextOptionsBuilder(SqlServerDbContextOptionsBuilder builder)
    {
        _builder = builder;
    }

    SqlServerDbContextOptionsBuilder ISqlServerUpsertableDbContextOptionsInfrastructure.OptionsBuilder => _builder;

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
        var relational = ((IRelationalDbContextOptionsBuilderInfrastructure)_builder).OptionsBuilder;
        var extension = relational.Options.FindExtension<SqlServerUpsertableDbContextOptionsExtension>() ?? new SqlServerUpsertableDbContextOptionsExtension();

        ((IDbContextOptionsBuilderInfrastructure)relational).AddOrUpdateExtension(configure(extension));

        return this;
    }
}