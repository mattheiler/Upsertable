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
        _converter = new GeometryValueConverter<TGeometry>(new SqlServerBytesReader(), new SqlServerBytesWriter());
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