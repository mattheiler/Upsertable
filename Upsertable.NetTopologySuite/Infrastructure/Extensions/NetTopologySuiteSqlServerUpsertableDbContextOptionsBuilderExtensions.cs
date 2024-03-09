using NetTopologySuite.Geometries;
using Upsertable.SqlServer.Infrastructure;
using Upsertable.SqlServer.NetTopologySuite.Data;

namespace Upsertable.SqlServer.NetTopologySuite.Infrastructure.Extensions;

public static class NetTopologySuiteSqlServerUpsertableDbContextOptionsBuilderExtensions
{
    public static SqlServerUpsertableDbContextOptionsBuilder UseNetTopologySuite(this SqlServerUpsertableDbContextOptionsBuilder @this)
    {
        return @this.DataResolver(_ => new SqlServerGeometryDataResolver<Geometry>());
    }
}