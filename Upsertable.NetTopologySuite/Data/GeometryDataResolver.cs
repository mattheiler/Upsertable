﻿using System;
using System.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.ValueConversion.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Upsertable.NetTopologySuite.Data;

public class GeometryDataResolver<TGeometry> : IDataResolver
    where TGeometry : Geometry
{
    private readonly ValueConverter _converter;

    public GeometryDataResolver()
    {
#pragma warning disable EF1001 // Internal EF Core API usage.
        _converter = new GeometryValueConverter<TGeometry>(new SqlServerBytesReader(), new SqlServerBytesWriter());
#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    public Type Type => typeof(TGeometry);

    public object? ResolveData(IProperty property, object? value)
    {
        return value == null ? SqlBytes.Null : _converter.ConvertToProvider(value);
    }

    public Type ResolveDataType(IProperty property)
    {
        return typeof(SqlBytes);
    }
}