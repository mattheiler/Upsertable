﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Upsertable.Extensions;

namespace Upsertable.SqlServer;

public class SourceTable
{
    private readonly IDataLoader _loaders;
    private readonly IDictionary<Type, IDataResolver> _resolvers;
    private readonly Source _source;

    public SourceTable(Source source, IDataLoader loaders, IEnumerable<IDataResolver> resolvers)
    {
        _source = source;
        _loaders = loaders;
        _resolvers = resolvers.ToDictionary(resolver => resolver.Type);
    }

    public async ValueTask DisposeAsync()
    {
        await _source.DropTableAsync();
    }

    public async Task LoadAsync(IEnumerable entities, DbConnection connection, DbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var table = CreateDataTable(_source, entities);
        await _loaders.LoadAsync(_source, table, connection, transaction, cancellationToken);
    }


    private DataTable CreateDataTable(Source source, IEnumerable entities)
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
                        row[property.GetColumnNameInTable()] = GetData(property, entity);
                        break;
                    case INavigation navigation:
                        var value = navigation.GetGetter().GetClrValue(entity) ?? throw new InvalidOperationException("Entity must not be null");
                        foreach (var property in navigation.TargetEntityType.GetProperties().Where(property => !property.IsPrimaryKey()))
                            row[property.GetColumnNameInTable()] = GetData(property, value);
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
            ColumnName = property.GetColumnNameInTable(),
            DataType = GetDataType(property)
        };
    }

    private Type GetDataType(IProperty property)
    {
        if (_resolvers.TryGetValue(property.ClrType, out var resolver))
            return resolver.ResolveDataType(property);

        return
            property.GetProviderClrType()
            ?? Nullable.GetUnderlyingType(property.ClrType)
            ?? property.ClrType;
    }

    private object? GetData(IProperty property, object entity)
    {
        var value = property.GetGetter().GetClrValue(entity);

        if (_resolvers.TryGetValue(property.ClrType, out var resolver))
            return resolver.ResolveData(property, value);

        var converter = property.GetValueConverter();
        var data = converter != null ? converter.ConvertToProvider(value) : value;
        return data ?? DBNull.Value;
    }
}