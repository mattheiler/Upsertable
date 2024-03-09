using System;
using System.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.ValueConversion.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Upsertable.SqlServer.NetTopologySuite.Data;

public class SqlServerGeometryDataResolver<TGeometry> : IDataResolver
    where TGeometry : Geometry
{
    private readonly ValueConverter _converter;

    public SqlServerGeometryDataResolver()
    {
#pragma warning disable EF1001 // Internal EF Core API usage.
        _converter = new GeometryValueConverter<TGeometry>(new SqlServerBytesReader(), new SqlServerBytesWriter());
#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    public Type Type => typeof(TGeometry);

    public object ResolveData(IProperty property, object value)
    {
        return value != null ? _converter.ConvertToProvider(value) : SqlBytes.Null;
    }

    public Type ResolveDataType(IProperty property)
    {
        return typeof(SqlBytes);
    }
}