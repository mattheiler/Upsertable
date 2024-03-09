using System;
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
using Upsertable.Data;
using Upsertable.Extensions;

namespace Upsertable.SqlServer;

public class SqlServerMerge : IMerge
{
    private readonly DbContext _db;
    private readonly SqlServerMergeOutput _output;
    private readonly EntityProviderFunc _provider;
    private readonly SqlServerMergeSource _source;
    private readonly IEntityType _target;

    public SqlServerMerge(DbContext db, IEntityType target, SqlServerMergeSource source, SqlServerMergeOutput output, EntityProviderFunc provider)
    {
        _db = db;
        _target = target;
        _source = source;
        _output = output;
        _provider = provider;
    }

    public List<IProperty> On { get; set; } = new();

    public List<IPropertyBase> Insert { get; } = new();

    public List<IPropertyBase> Update { get; } = new();

    public List<INavigation> Dependents { get; } = new();

    public List<INavigation> Principals { get; } = new();

    public bool IsReadOnly { get; set; }

    public MergeBehavior Behavior { get; set; }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var connection = _db.Database.GetDbConnection();
        var transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
        if (connection.State == ConnectionState.Closed)
            await connection.OpenAsync(cancellationToken);

        var entities = _provider().Cast<object>().ToList();

        await PreProcessAsync(entities, connection, transaction, cancellationToken);

        await using var source = await _source.CreateTableAsync(cancellationToken);
        await using var output = await _output.CreateTableAsync(cancellationToken);

        await source.LoadAsync(entities, connection, transaction, cancellationToken);

        if (!IsReadOnly)
            await ProcessAsync(cancellationToken);

        await PostProcessAsync(entities, connection, transaction, cancellationToken);
    }


    protected Task PreProcessAsync(IEnumerable<object> entities, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
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
        await _db.Database.ExecuteSqlRawAsync(ToSql(), cancellationToken);
    }

    protected async Task PostProcessAsync(IEnumerable<object> entities, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var properties = On.Union(_target.GetKeys().SelectMany(key => key.Properties).Distinct()).ToList();
        var statement = $"SELECT {string.Join(", ", properties.Select(property => $"[{property.GetColumnNameInTable()}]"))} FROM [{_output.GetTableName()}]";

        await using var command = new SqlCommand(statement, (SqlConnection)connection, (SqlTransaction)transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!reader.HasRows)
            return;

        var lookup = entities.ToDictionary(On.GetValues, EntityEqualityComparer.Default);
        var values = new object[properties.Count];

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
            .AppendLine($"MERGE [{_target.GetTableName()}] AS [T]")
            .AppendLine($"USING [{_source.GetTableName()}] AS [S]");

        var on = On.Select(property => property.GetColumnNameInTable()).Select(column => $"[T].[{column}] = [S].[{column}]");

        command.AppendLine($"ON {string.Join(" AND ", on)}");

        if (Behavior.HasFlag(MergeBehavior.Update))
        {
            var columns = GetColumnsForUpdate();

            command.AppendLine($"WHEN MATCHED THEN UPDATE SET {string.Join(", ", columns.Select(column => $"[T].[{column}] = [S].[{column}]"))}");
        }
        else
        {
            command.AppendLine("WHEN MATCHED THEN UPDATE SET @UPDATE = 1");
        }

        if (Behavior.HasFlag(MergeBehavior.Insert))
        {
            var columns = GetColumnsForInsert().ToList();

            command.AppendLine($"WHEN NOT MATCHED BY TARGET THEN INSERT ({string.Join(", ", columns)}) VALUES ({string.Join(", ", columns.Select(column => $"[S].[{column}]"))})");
        }

        var output =
            _output
                .GetProperties()
                .Union(_target.GetKeys().SelectMany(key => key.Properties).Distinct())
                .Select(property => property.GetColumnNameInTable())
                .Select(column => $"INSERTED.[{column}] AS [{column}]");

        command
            .AppendLine($"OUTPUT {string.Join(", ", output)} INTO [{_output.GetTableName()}]")
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
            IProperty property => GetColumnsForUpdate(new[] { property }),
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
            IProperty property => GetColumnsForInsert(new[] { property }),
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

    private class EntityEqualityComparer : EqualityComparer<IEnumerable<object>>
    {
        public new static readonly EntityEqualityComparer Default = new();

        private EntityEqualityComparer()
        {
        }

        public override bool Equals(IEnumerable<object> x, IEnumerable<object> y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return x.SequenceEqual(y);
        }

        public override int GetHashCode(IEnumerable<object> obj)
        {
            return obj.Aggregate(0, HashCode.Combine);
        }
    }
}