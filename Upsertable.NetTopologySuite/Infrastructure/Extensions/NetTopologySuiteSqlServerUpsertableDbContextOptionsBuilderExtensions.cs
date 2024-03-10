using NetTopologySuite.Geometries;
using Upsertable.Infrastructure;
using Upsertable.NetTopologySuite.Data;

namespace Upsertable.NetTopologySuite.Infrastructure.Extensions;

public static class NetTopologySuiteSqlServerUpsertableDbContextOptionsBuilderExtensions
{
    public static SqlServerUpsertableDbContextOptionsBuilder UseNetTopologySuite(this SqlServerUpsertableDbContextOptionsBuilder @this)
    {
        return @this.DataResolver(_ => new GeometryDataResolver<Geometry>());
    }
}