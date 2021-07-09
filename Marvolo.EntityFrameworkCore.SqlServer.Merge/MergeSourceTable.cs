using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeSourceTable : IAsyncDisposable
    {
        private readonly IMergeSourceLoader _loader;
        private readonly MergeSource _source;

        public MergeSourceTable(MergeSource source, IMergeSourceLoader loader)
        {
            _source = source;
            _loader = loader;
        }

        public async Task LoadAsync(IEnumerable entities, CancellationToken cancellationToken = default)
        {
            var table = GetDataTable(_source.EntityType, entities);
            var database = _source.Context.Database;
            var connection = (SqlConnection) database.GetDbConnection();
            var transaction = (SqlTransaction) database.CurrentTransaction?.GetDbTransaction();

            await _loader.ExecuteAsync(_source, table, connection, transaction, cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return DropAsync();
        }

        internal async Task CreateAsync(CancellationToken cancellationToken = default)
        {
            var columns = new List<string>();

            foreach (var column in _source.EntityType.GetColumns())
                switch (column)
                {
                    case IProperty property:
                        columns.Add($"[{property.GetColumnName()}] {property.GetColumnType()}");
                        break;
                    case INavigation navigation:
                        columns.AddRange(navigation.GetColumns().Select(property => $"[{property.GetColumnName()}] {property.GetColumnType()}"));
                        break;
                    default:
                        throw new NotSupportedException("Property or navigation type not supported.");
                }

            var command = $"CREATE TABLE {_source.GetTableName()} ({string.Join(", ", columns)})";

            await _source.Context.Database.ExecuteSqlRawAsync(command, cancellationToken);
        }

        private async ValueTask DropAsync()
        {
            await _source.Context.Database.ExecuteSqlRawAsync($"DROP TABLE {_source.GetTableName()}");
        }

        private static DataTable GetDataTable(IEntityType type, IEnumerable entities)
        {
            var table = new DataTable();
            var members = type.GetColumns().ToList();

            foreach (var member in members)
                switch (member)
                {
                    case IProperty property:
                        table.Columns.Add(GetDataColumn(property));
                        break;
                    case INavigation navigation:
                        foreach (var property in navigation.GetColumns())
                            table.Columns.Add(GetDataColumn(property));
                        break;
                    default:
                        throw new NotSupportedException("Property or navigation type not supported.");
                }

            foreach (var entity in entities)
            {
                var row = table.NewRow();

                foreach (var member in members)
                    switch (member)
                    {
                        case IProperty property:
                            row[property.GetColumnName()] = GetData(property, entity);
                            break;
                        case INavigation navigation:
                            var value = navigation.GetGetter().GetClrValue(entity);
                            foreach (var property in navigation.GetColumns())
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