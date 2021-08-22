using System;
using System.Collections;
using System.Data;
using System.Linq;
using Marvolo.EntityFramework.SqlMerge.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFramework.SqlMerge
{
    public class MergeSourceBuilder : IMergeSourceBuilder
    {
        public DataTable GetDataTable(MergeSource source, IEnumerable entities)
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
                            row[property.GetColumnName()] = GetData(property, entity);
                            break;
                        case INavigation navigation:
                            var value = navigation.GetGetter().GetClrValue(entity);
                            foreach (var property in navigation.TargetEntityType.GetProperties().Where(property => !property.IsPrimaryKey())) 
                                row[property.GetColumnName()] = GetData(property, value);
                            break;
                        default:
                            throw new NotSupportedException("Property or navigation type not supported.");
                    }

                table.Rows.Add(row);
            }

            return table;
        }

        private static DataColumn GetDataColumn(IProperty property)
        {
            return new DataColumn
            {
                AllowDBNull = property.IsColumnNullable(),
                ColumnName = property.GetColumnName(),
                DataType = GetDataType(property)
            };
        }

        private static Type GetDataType(IProperty property)
        {
            return property.GetMergeProviderClrType() ?? property.GetProviderClrType() ?? Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        }

        private static object GetData(IProperty property, object entity)
        {
            var value = property.GetGetter().GetClrValue(entity);
            var converter = property.GetMergeValueConverter() ?? property.GetValueConverter();
            var data = converter != null ? converter.ConvertToProvider(value) : value;
            return data ?? DBNull.Value;
        }
    }
}