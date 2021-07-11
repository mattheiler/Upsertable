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
            // update foreign keys from the principal entities

            var entities = context.Get(_target.EntityType.ClrType);
            var navigations = _target.EntityType.GetNavigations().Where(navigation => navigation.IsDependentToPrincipal() && !navigation.DeclaringEntityType.IsOwned()).ToList();

            // TODO add scope to the context, so all entities aren't evaluated

            foreach (var entity in entities)
            foreach (var navigation in navigations)
            {
                // update foreign key

                var value = navigation.GetValue(entity);
                if (value != null)
                    navigation.ForeignKey.SetValues(entity, navigation.ForeignKey.PrincipalKey.GetValues(value));
            }

            return Task.CompletedTask;
        }

        protected virtual async Task PostProcessAsync(MergeContext context, SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken = default)
        {
            // update keys and foreign keys from the output values

            var on = _on.Properties;
            var keys = _target.EntityType.FindPrimaryKey().Properties;
            var properties = on.Concat(keys).ToList();

            var statement = $"SELECT {string.Join(", ", properties.Select(property => $"[{property.GetColumnName()}]"))} FROM [{_output.GetTableName()}] WHERE [{_output.GetActionName()}] IN ('INSERT', 'UPDATE')";

            await using var command = new SqlCommand(statement, connection, transaction);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!reader.HasRows)
                return;

            var entities = context.Get(_target.EntityType.ClrType).Cast<object>().ToDictionary(on.GetValues, MergeOnEqualityComparer.Default);
            var navigations = _target.EntityType.GetNavigations().Where(navigation => !navigation.IsDependentToPrincipal() && !navigation.ForeignKey.DeclaringEntityType.IsOwned()).ToList();

            // TODO manage the set of navigations

            var offset = properties.Count - keys.Count;
            var values = new object[properties.Count];
            
            while (await reader.ReadAsync(cancellationToken))
            {
                // read values

                if (reader.GetValues(values) != values.Length)
                    throw new InvalidOperationException("Read an incorrect number of columns.");

                // convert values

                for (var index = 0; index < values.Length; index++)
                {
                    var property = properties[index];

                    var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                    if (type.IsEnum)
                        values[index] = Enum.ToObject(type, values[index]);

                    var converter = property.GetValueConverter();
                    if (converter != null)
                        values[index] = converter.ConvertFromProvider(values[index]);
                }

                // find by values

                if (!entities.TryGetValue(values.Take(on.Count), out var entity))
                    throw new InvalidOperationException("Couldn't find the original entity.");

                // TODO don't update the entities themselves - index and provide to source loader through... context???

                // update keys

                for (var index = offset; index < properties.Count; index++)
                    properties[index].SetValue(entity, values[index]);

                // update foreign keys

                foreach (var navigation in navigations)
                {
                    var value = navigation.GetValue(entity);
                    if (value == null)
                        continue;

                    if (navigation.IsCollection())
                        foreach (var item in (IEnumerable) value)
                            navigation.ForeignKey.SetValues(item, values, offset);
                    else
                        navigation.ForeignKey.SetValues(value, values, offset);
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

            var conditions =
                _on
                    .Properties
                    .Select(property => property.GetColumnName())
                    .Select(column => $"[T].[{column}] = [S].[{column}]");

            command.AppendLine($"ON {string.Join(" AND ", conditions)}");

            if (_behavior.HasFlag(MergeBehavior.WhenMatchedThenUpdate))
            {
                var columns = new List<string>();

                foreach (var update in _update.Properties)
                {
                    switch (update)
                    {
                        case IProperty property:
                            if (!property.ValueGenerated.HasFlag(ValueGenerated.OnUpdate) && property.GetValueGenerationStrategy() != SqlServerValueGenerationStrategy.IdentityColumn)
                                columns.Add(property.GetColumnName());
                            break;
                        case INavigation navigation:
                            var properties =
                                from property in navigation.GetTargetType().GetProperties().Where(property => !property.IsPrimaryKey())
                                where !property.ValueGenerated.HasFlag(ValueGenerated.OnUpdate) && property.GetValueGenerationStrategy() != SqlServerValueGenerationStrategy.IdentityColumn
                                select property;
                            columns.AddRange(properties.Select(property => property.GetColumnName()));
                            break;
                        default:
                            throw new NotSupportedException("Property or navigation type not supported.");
                    }
                }

                command.AppendLine($"WHEN MATCHED THEN UPDATE SET {string.Join(", ", columns.Select(column => $"[T].[{column}] = [S].[{column}]"))}");
            }
            else
            {
                command.AppendLine("WHEN MATCHED THEN UPDATE SET @UPDATE = 1");
            }

            if (_behavior.HasFlag(MergeBehavior.WhenNotMatchedByTargetThenInsert))
            {
                var columns = new List<string>();

                foreach (var insert in _insert.Properties)
                {
                    switch (insert)
                    {
                        case IProperty property:
                            if (!property.ValueGenerated.HasFlag(ValueGenerated.OnAdd) && property.GetValueGenerationStrategy() != SqlServerValueGenerationStrategy.IdentityColumn)
                                columns.Add(property.GetColumnName());
                            break;
                        case INavigation navigation:
                            var properties =
                                from property in navigation.GetTargetType().GetProperties().Where(property => !property.IsPrimaryKey())
                                where !property.ValueGenerated.HasFlag(ValueGenerated.OnAdd) && property.GetValueGenerationStrategy() != SqlServerValueGenerationStrategy.IdentityColumn
                                select property;
                            columns.AddRange(properties.Select(property => property.GetColumnName()));
                            break;
                        default:
                            throw new NotSupportedException("Property or navigation type not supported.");
                    }
                }

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
                    .Select(column => $"INSERTED.[{column}] AS [{column}]")
                    .Append($"$action AS [{_output.GetActionName()}]");

            command
                .AppendLine($"OUTPUT {string.Join(", ", output)} INTO [{_output.GetTableName()}]")
                .AppendLine(";");

            return command.ToString();
        }

        public override string ToString()
        {
            return ToSql();
        }
    }
}