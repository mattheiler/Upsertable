using Upsertable.SqlServer.Infrastructure;

namespace Upsertable.SqlServer.NetTopologySuite.Infrastructure
{
    public interface INetTopologySuiteSqlServerUpsertableDbContextOptionsInfrastructure
    {
        MergeSqlServerDbContextOptionsBuilder OptionsBuilder { get; }
    }
}