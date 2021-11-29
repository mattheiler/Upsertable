using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Upsertable.SqlServer.Infrastructure;

namespace Upsertable.SqlServer.NetTopologySuite.Infrastructure.Extensions
{
    public static class NetTopologySuiteSqlServerUpsertableDbContextOptionsBuilderExtensions
    {
        public static MergeSqlServerDbContextOptionsBuilder UseNetTopologySuite(this MergeSqlServerDbContextOptionsBuilder @this, Action<NetTopologySuiteSqlServerUpsertableDbContextOptionsBuilder> configure = default)
        {
            var upsertable = ((IMergeSqlServerDbContextOptionsInfrastructure)@this).OptionsBuilder;
            var relational = ((IRelationalDbContextOptionsBuilderInfrastructure)upsertable).OptionsBuilder;
            var extension = relational.Options.FindExtension<MergeSqlServerDbContextOptionsExtension>() ?? new MergeSqlServerDbContextOptionsExtension();

            ((IDbContextOptionsBuilderInfrastructure)relational).AddOrUpdateExtension(extension);

            configure?.Invoke(new NetTopologySuiteSqlServerUpsertableDbContextOptionsBuilder(@this));

            return @this;
        }
    }
}