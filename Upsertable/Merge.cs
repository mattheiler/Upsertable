﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Upsertable.Internal.Extensions;

namespace Upsertable;

public class Merge(DbContext db, Source source, IEntityType target, Output output, EntityProviderFunc provider) : IMerge
{
    public List<IProperty> On { get; set; } = [];

    public List<IPropertyBase> Insert { get; } = [];

    public List<IPropertyBase> Update { get; } = [];

    public List<INavigation> Dependents { get; } = [];

    public List<INavigation> Principals { get; } = [];

    public bool IsReadOnly { get; set; }

    public MergeBehavior Behavior { get; set; }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();
        var transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        if (connection.State == ConnectionState.Closed)
            await connection.OpenAsync(cancellationToken);

        var entities = provider().Cast<object>().ToList();

        await PreProcessAsync(entities);

        await using var sourceTable = await source.CreateTableAsync(cancellationToken);
        await using var outputTable = await output.CreateTableAsync(cancellationToken);

        await sourceTable.LoadAsync(entities, connection, transaction, cancellationToken);

        if (!IsReadOnly) await ProcessAsync(cancellationToken);

        await PostProcessAsync(entities, connection, transaction, cancellationToken);
    }

    protected Task PreProcessAsync(IEnumerable<object> entities)
    {
        foreach (var entity in entities)
        foreach (var navigation in Principals)
        {
            var value = navigation.GetValue(entity);
            if (value != null)
                navigation.ForeignKey.Properties.SetValues(entity, navigation.ForeignKey.PrincipalKey.Properties.GetValues(value));
        }

        return Task.CompletedTask;
    }

    protected async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(ToSql(), cancellationToken);
    }

    protected async Task PostProcessAsync(IEnumerable<object> entities, DbConnection connection, DbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var properties = On.Union(target.GetKeys().SelectMany(key => key.Properties).Distinct()).ToList();
        var statement = $"SELECT {string.Join(", ", properties.Select(property => $"[{property.GetColumnNameInTable()}]"))} FROM [{output.GetTableName()}]";

        await using var command = new SqlCommand(statement, (SqlConnection)connection, (SqlTransaction?)transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!reader.HasRows)
            return;

        var lookup = entities.ToDictionary(On.GetValues, EntityEqualityComparer.Default);
        var values = new object?[properties.Count];

        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.GetValues(values) != values.Length)
                throw new InvalidOperationException("Read an incorrect number of columns.");

            for (var index = 0; index < properties.Count; index++)
            {
                var property = properties[index];
                var value = values[index];
                if (value is DBNull)
                    value = null;

                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (type.IsEnum)
                    value = value != null ? Enum.ToObject(type, value) : null;

                var converter = property.GetValueConverter();
                if (converter != null)
                    value = converter.ConvertFromProvider(value);

                values[index] = value;
            }

            if (!lookup.TryGetValue(values.Take(On.Count), out var entity))
                throw new InvalidOperationException("Couldn't find the original entity.");

            properties.SetValues(entity, values);

            foreach (var navigation in Dependents)
            {
                var value = navigation.GetValue(entity);
                if (value == null)
                    continue;

                if (navigation.IsCollection)
                    foreach (var item in (IEnumerable)value)
                        navigation.ForeignKey.Properties.SetValues(item, navigation.ForeignKey.PrincipalKey.Properties.GetValues(entity));
                else
                    navigation.ForeignKey.Properties.SetValues(value, navigation.ForeignKey.PrincipalKey.Properties.GetValues(entity));
            }
        }
    }

    private string ToSql()
    {
        var command = new StringBuilder();

        command
            .AppendLine("DECLARE @UPDATE bit")
            .AppendLine(";");

        command
            .AppendLine($"MERGE [{target.GetTableName()}] AS [T]")
            .AppendLine($"USING [{source.GetTableName()}] AS [S]");

        var on = On.Select(property => property.GetColumnNameInTable()).Select(column => $"[T].[{column}] = [S].[{column}]");

        command.AppendLine($"ON {string.Join(" AND ", on)}");

        if (Behavior.HasFlag(MergeBehavior.Update))
        {
            var updateColumns = GetColumnsForUpdate();

            command.AppendLine($"WHEN MATCHED THEN UPDATE SET {string.Join(", ", updateColumns.Select(column => $"[T].[{column}] = [S].[{column}]"))}");
        }
        else
        {
            command.AppendLine("WHEN MATCHED THEN UPDATE SET @UPDATE = 1");
        }

        if (Behavior.HasFlag(MergeBehavior.Insert))
        {
            var insertColumns = GetColumnsForInsert().ToList();

            command.AppendLine($"WHEN NOT MATCHED BY TARGET THEN INSERT ({string.Join(", ", insertColumns)}) VALUES ({string.Join(", ", insertColumns.Select(column => $"[S].[{column}]"))})");
        }

        var outputColumns =
            output
                .GetProperties()
                .Union(target.GetKeys().SelectMany(key => key.Properties).Distinct())
                .Select(property => property.GetColumnNameInTable())
                .Select(column => $"INSERTED.[{column}] AS [{column}]");

        command
            .AppendLine($"OUTPUT {string.Join(", ", outputColumns)} INTO [{output.GetTableName()}]")
            .AppendLine(";");

        return command.ToString();
    }

    private IEnumerable<string> GetColumnsForUpdate()
    {
        return Update.SelectMany(GetColumnsForUpdate);
    }

    private static IEnumerable<string> GetColumnsForUpdate(IPropertyBase member)
    {
        return member switch
        {
            IProperty property => GetColumnsForUpdate([property]),
            INavigation navigation => GetColumnsForUpdate(navigation.TargetEntityType.GetProperties()),
            _ => throw new NotSupportedException("Property or navigation type not supported.")
        };
    }

    private static IEnumerable<string> GetColumnsForUpdate(IEnumerable<IProperty> properties)
    {
        return
            from property in properties
            where !property.ValueGenerated.HasFlag(ValueGenerated.OnAdd)
            where property.GetValueGenerationStrategy() != SqlServerValueGenerationStrategy.IdentityColumn
            where !property.IsShadowProperty()
            select property.GetColumnNameInTable();
    }

    private IEnumerable<string> GetColumnsForInsert()
    {
        return Insert.SelectMany(GetColumnsForInsert);
    }

    private static IEnumerable<string> GetColumnsForInsert(IPropertyBase member)
    {
        return member switch
        {
            IProperty property => GetColumnsForInsert([property]),
            INavigation navigation => GetColumnsForInsert(navigation.TargetEntityType.GetProperties()),
            _ => throw new NotSupportedException("Property or navigation type not supported.")
        };
    }

    private static IEnumerable<string> GetColumnsForInsert(IEnumerable<IProperty> properties)
    {
        return
            from property in properties
            where !property.ValueGenerated.HasFlag(ValueGenerated.OnUpdate)
            where property.GetValueGenerationStrategy() != SqlServerValueGenerationStrategy.IdentityColumn
            where !property.IsShadowProperty()
            select property.GetColumnNameInTable();
    }

    public override string ToString()
    {
        return ToSql();
    }

    private class EntityEqualityComparer : EqualityComparer<IEnumerable<object?>>
    {
        public new static readonly EntityEqualityComparer Default = new();

        private EntityEqualityComparer()
        {
        }

        public override bool Equals(IEnumerable<object?>? x, IEnumerable<object?>? y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return x.SequenceEqual(y);
        }

        public override int GetHashCode(IEnumerable<object?> obj)
        {
            return obj.Aggregate(0, HashCode.Combine);
        }
    }
}