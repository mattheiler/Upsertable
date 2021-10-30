using System;
using System.Collections;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Upsertable.Abstractions;

namespace Upsertable.Data
{
    public class DataTableFactory : IDataTableFactory
    {
        private readonly DataResolverProvider _resolvers;

        public DataTableFactory(DataResolverProvider resolvers)
        {
            _resolvers = resolvers;
        }

        public DataTable CreateDataTable(IMergeSource source, IEnumerable entities)
        {
            var table = new DataTable();
            var properties = source.GetProperties().ToList();

            foreach (var member in properties)
                switch (member)
                {
                    case IProperty property:
                        table.Columns.Add(GetDataColumn(property));
                        break;
                    case INavigation navigation:
                        foreach (var property in navigation.TargetEntityType.GetProperties().Where(property => !property.IsPrimaryKey()))
                            table.Columns.Add(GetDataColumn(property));
                        break;
                    default:
                        throw new NotSupportedException("Property or navigation type not supported.");
                }

            foreach (var entity in entities)
            {
                var row = table.NewRow();

                foreach (var member in properties)
                    switch (member)
                    {
                        case IProperty property:
                            row[property.GetColumnBaseName()] = GetData(property, entity);
                            break;
                        case INavigation navigation:
                            var value = navigation.GetGetter().GetClrValue(entity);
                            foreach (var property in navigation.TargetEntityType.GetProperties().Where(property => !property.IsPrimaryKey()))
                                row[property.GetColumnBaseName()] = GetData(property, value);
                            break;
                        default:
                            throw new NotSupportedException("Property or navigation type not supported.");
                    }

                table.Rows.Add(row);
            }

            return table;
        }

        private DataColumn GetDataColumn(IProperty property)
        {
            return new DataColumn
            {
                AllowDBNull = property.IsColumnNullable(),
                ColumnName = property.GetColumnBaseName(),
                DataType = GetDataType(property)
            };
        }

        private Type GetDataType(IProperty property)
        {
            var resolver = _resolvers.GetResolver(property.ClrType);
            if (resolver != null)
                return resolver.ResolveDataType(property);

            return
                property.GetProviderClrType()
                ?? Nullable.GetUnderlyingType(property.ClrType)
                ?? property.ClrType;
        }

        private object GetData(IProperty property, object entity)
        {
            var value = property.GetGetter().GetClrValue(entity);

            var resolver = _resolvers.GetResolver(property.ClrType);
            if (resolver != null)
                return resolver.ResolveData(property, value);

            var converter = property.GetValueConverter();
            var data = (converter != null ? converter.ConvertToProvider(value) : value) ?? DBNull.Value;
            return data;
        }
    }
}