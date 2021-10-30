using System;
using System.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.ValueConversion.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Upsertable.EntityFramework.Data;

namespace Upsertable.EntityFramework.SqlServer.NetTopologySuite.Resolvers
{
    public class SqlServerGeometryDataResolver<TGeometry> : DataResolver<TGeometry>
        where TGeometry : Geometry
    {
        private readonly ValueConverter _converter;

        public SqlServerGeometryDataResolver()
        {
#pragma warning disable EF1001 // Internal EF Core API usage.
            _converter = new GeometryValueConverter<TGeometry>(new SqlServerBytesReader(), new SqlServerBytesWriter());
#pragma warning restore EF1001 // Internal EF Core API usage.
        }

        public override object ResolveData(IProperty property, object value)
        {
            return value != null ? _converter.ConvertToProvider(value) : SqlBytes.Null;
        }

        public override Type ResolveDataType(IProperty property)
        {
            return typeof(SqlBytes);
        }
    }
}