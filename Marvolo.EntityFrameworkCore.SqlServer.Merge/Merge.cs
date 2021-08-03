using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class Merge : IMerge
    {
        private readonly MergeBehavior _behavior;
        private readonly DbContext _db;
        private readonly MergeInsert _insert;
        private readonly MergeOn _on;
        private readonly MergeOutput _output;
        private readonly MergeSource _source;
        private readonly MergeTarget _target;
        private readonly MergeUpdate _update;

        public Merge(DbContext db, MergeTarget target, MergeSource source, MergeOn on, MergeBehavior behavior, MergeInsert insert, MergeUpdate update, MergeOutput output)
        {
            _db = db;
            _target = target;
            _source = source;
            _on = on;
            _behavior = behavior;
            _insert = insert;
            _update = update;
            _output = output;
        }

        public async Task ExecuteAsync(MergeContext context, CancellationToken cancellationToken = default)
        {
            var connection = (SqlConnection) _db.Database.GetDbConnection();
            var transaction = (SqlTransaction) _db.Database.CurrentTransaction?.GetDbTransaction();
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken);

            await PreProcessAsync(context, connection, transaction, cancellationToken);

            await using var source = await _source.CreateTableAsync(cancellationToken);
            await using var output = await _output.CreateTableAsync(cancellationToken);

            await source.LoadAsync(context.Get(_target.EntityType.ClrType), connection, transaction, cancellationToken);

            await _db.Database.ExecuteSqlRawAsync(ToSql(), cancellationToken);

            await PostProcessAsync(context, connection, transaction, cancellationToken);
        }

        protected virtual Task PreProcessAsync(MergeContext context, SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken = default)
        {
            var entities = context.Get(_target.EntityType.ClrType);
            var navigations = 
                _target
                    .EntityType
                    .GetNavigations()
                    .Where(navigation => navigation.IsDependentToPrincipal())
                    .Where(navigation => !navigation.DeclaringEntityType.IsOwned())
                    .Where(navigation => context.Contains(navigation.DeclaringEntityType.ClrType))
                    .ToList();

            foreach (var entity in entities)
            foreach (var navigation in navigations)
            {
                var value = navigation.GetValue(entity);
                if (value != null)
                    navigation.ForeignKey.Properties.SetValues(entity, navigation.ForeignKey.PrincipalKey.Properties.GetValues(value));
            }

            return Task.CompletedTask;
        }

        protected virtual async Task PostProcessAsync(MergeContext context, SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken = default)
        {
            var properties = _on.Properties.Union(_target.EntityType.GetKeys().SelectMany(key => key.Properties)).Distinct().ToList();
            var statement = $"SELECT {string.Join(", ", properties.Select(property => $"[{property.GetColumnName()}]"))} FROM [{_output.GetTableName()}]";

            await using var command = new SqlCommand(statement, connection, transaction);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!reader.HasRows)
                return;

            var entities = context.Get(_target.EntityType.ClrType).Cast<object>().ToDictionary(_on.Properties.GetValues, MergeOnEqualityComparer.Default);
            var navigations = 
                _target
                    .EntityType
                    .GetNavigations()
                    .Where(navigation => !navigation.IsDependentToPrincipal())
                    .Where(navigation => !navigation.ForeignKey.DeclaringEntityType.IsOwned())
                    .Where(navigation => context.Contains(navigation.ForeignKey.DeclaringEntityType.ClrType))
                    .ToList();

            var values = new object[properties.Count];

            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.GetValues(values) != values.Length)
                    throw new InvalidOperationException("Read an incorrect number of columns.");

                for (var index = 0; index < properties.Count; index++)
                {
                    var property = properties[index];
                    var value = values[index];

                    var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                    if (type.IsEnum)
                        value = Enum.ToObject(type, value);

                    var converter = property.GetValueConverter();
                    if (converter != null)
                        value = converter.ConvertFromProvider(value);

                    values[index] = value;
                }

                if (!entities.TryGetValue(values.Take(_on.Properties.Count), out var entity))
                    throw new InvalidOperationException("Couldn't find the original entity.");

                properties.SetValues(entity, values);

                foreach (var navigation in navigations)
                {
                    var value = navigation.GetValue(entity);
                    if (value == null)
                        continue;

                    if (navigation.IsCollection())
                        foreach (var item in (IEnumerable) value)
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

            var on = _on.Properties.Select(property => property.GetColumnName()).Select(column => $"[T].[{column}] = [S].[{column}]");

            command.AppendLine($"ON {string.Join(" AND ", on)}");

            if (_behavior.HasFlag(MergeBehavior.WhenMatchedThenUpdate))
            {
                var columns = GetColumnsForUpdate();

                command.AppendLine($"WHEN MATCHED THEN UPDATE SET {string.Join(", ", columns.Select(column => $"[T].[{column}] = [S].[{column}]"))}");
            }
            else
            {
                command.AppendLine("WHEN MATCHED THEN UPDATE SET @UPDATE = 1");
            }

            if (_behavior.HasFlag(MergeBehavior.WhenNotMatchedByTargetThenInsert))
            {
                var columns = GetColumnsForInsert().ToList();

                command.AppendLine($"WHEN NOT MATCHED BY TARGET THEN INSERT ({string.Join(", ", columns)}) VALUES ({string.Join(", ", columns.Select(column => $"[S].[{column}]"))})");
            }

            if (_behavior.HasFlag(MergeBehavior.WhenNotMatchedBySourceThenDelete))
            {
                command.AppendLine("WHEN NOT MATCHED BY SOURCE THEN DELETE");
            }

            var output =
                _output
                    .GetProperties()
                    .Select(property => property.GetColumnName())
                    .Select(column => $"INSERTED.[{column}] AS [{column}]");

            command
                .AppendLine($"OUTPUT {string.Join(", ", output)} INTO [{_output.GetTableName()}]")
                .AppendLine(";");

            return command.ToString();
        }

        private IEnumerable<string> GetColumnsForUpdate()
        {
            return _update.Properties.SelectMany(GetColumnsForUpdate);
        }

        private static IEnumerable<string> GetColumnsForUpdate(IPropertyBase member)
        {
            return member switch
            {
                IProperty property => GetColumnsForUpdate(new[] { property }),
                INavigation navigation => GetColumnsForUpdate(navigation.GetTargetType().GetProperties()),
                _ => throw new NotSupportedException("Property or navigation type not supported.")
            };
        }

        private static IEnumerable<string> GetColumnsForUpdate(IEnumerable<IProperty> properties)
        {
            return
                from property in properties
                where !property.ValueGenerated.HasFlag(ValueGenerated.OnAdd)
                where property.GetValueGenerationStrategy() != SqlServerValueGenerationStrategy.IdentityColumn
                select property.GetColumnName();
        }

        private IEnumerable<string> GetColumnsForInsert()
        {
            return _insert.Properties.SelectMany(GetColumnsForInsert);
        }

        private static IEnumerable<string> GetColumnsForInsert(IPropertyBase member)
        {
            return member switch
            {
                IProperty property => GetColumnsForInsert(new[] { property }),
                INavigation navigation => GetColumnsForInsert(navigation.GetTargetType().GetProperties()),
                _ => throw new NotSupportedException("Property or navigation type not supported.")
            };
        }

        private static IEnumerable<string> GetColumnsForInsert(IEnumerable<IProperty> properties)
        {
            return
                from property in properties
                where !property.ValueGenerated.HasFlag(ValueGenerated.OnUpdate)
                where property.GetValueGenerationStrategy() != SqlServerValueGenerationStrategy.IdentityColumn
                select property.GetColumnName();
        }

        public override string ToString()
        {
            return ToSql();
        }
    }
}