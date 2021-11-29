using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Upsertable.Abstractions;
using Upsertable.Data;

namespace Upsertable.SqlServer.Infrastructure
{
    public class SqlServerUpsertableDbContextOptionsBuilder : ISqlServerUpsertableDbContextOptionsInfrastructure
    {
        private readonly SqlServerDbContextOptionsBuilder _optionsBuilder;

        public SqlServerUpsertableDbContextOptionsBuilder(SqlServerDbContextOptionsBuilder optionsBuilder)
        {
            _optionsBuilder = optionsBuilder;
        }

        SqlServerDbContextOptionsBuilder ISqlServerUpsertableDbContextOptionsInfrastructure.OptionsBuilder => _optionsBuilder;

        public SqlServerUpsertableDbContextOptionsBuilder SourceLoader(Func<IServiceProvider, IDataTableLoader> factory)
        {
            return WithOption(e => e.WithSourceLoader(factory));
        }

        public SqlServerUpsertableDbContextOptionsBuilder DataResolver(Func<IServiceProvider, IDataResolver> factory)
        {
            return WithOption(e => e.WithDataResolver(factory));
        }

        private SqlServerUpsertableDbContextOptionsBuilder WithOption(Func<SqlServerUpsertableDbContextOptionsExtension, SqlServerUpsertableDbContextOptionsExtension> configure)
        {
            var relational = ((IRelationalDbContextOptionsBuilderInfrastructure)_optionsBuilder).OptionsBuilder;
            var extension = relational.Options.FindExtension<SqlServerUpsertableDbContextOptionsExtension>() ?? new SqlServerUpsertableDbContextOptionsExtension();

            ((IDbContextOptionsBuilderInfrastructure)relational).AddOrUpdateExtension(configure(extension));

            return this;
        }
    }
}