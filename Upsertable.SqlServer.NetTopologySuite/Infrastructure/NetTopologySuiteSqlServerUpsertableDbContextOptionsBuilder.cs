using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Upsertable.SqlServer.Infrastructure;

namespace Upsertable.SqlServer.NetTopologySuite.Infrastructure
{
    public class NetTopologySuiteSqlServerUpsertableDbContextOptionsBuilder : INetTopologySuiteSqlServerUpsertableDbContextOptionsInfrastructure
    {
        private readonly MergeSqlServerDbContextOptionsBuilder _optionsBuilder;

        public NetTopologySuiteSqlServerUpsertableDbContextOptionsBuilder(MergeSqlServerDbContextOptionsBuilder optionsBuilder)
        {
            _optionsBuilder = optionsBuilder;
        }

        MergeSqlServerDbContextOptionsBuilder INetTopologySuiteSqlServerUpsertableDbContextOptionsInfrastructure.OptionsBuilder => _optionsBuilder;


        private NetTopologySuiteSqlServerUpsertableDbContextOptionsBuilder WithOption(Func<MergeSqlServerDbContextOptionsExtension, MergeSqlServerDbContextOptionsExtension> configure)
        {
            var upsertable = ((IMergeSqlServerDbContextOptionsInfrastructure)_optionsBuilder).OptionsBuilder;
            var relational = ((IRelationalDbContextOptionsBuilderInfrastructure)upsertable).OptionsBuilder;
            var extension = relational.Options.FindExtension<MergeSqlServerDbContextOptionsExtension>() ?? new MergeSqlServerDbContextOptionsExtension();

            ((IDbContextOptionsBuilderInfrastructure)relational).AddOrUpdateExtension(configure(extension));

            return this;
        }
    }
}