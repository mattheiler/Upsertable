using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Internal;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class Merge : IMerge
    {
        private readonly DbContext _db;
        private readonly MergeTarget _target;
        private readonly MergeSource _source;
        private readonly MergeOn _on;
        private readonly MergeBehavior _behavior;
        private readonly MergeInsert _insert;
        private readonly MergeUpdate _update;
        private readonly MergeOutput _output;

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
            var connection = _db.Database.GetDbConnection();
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken);

            await using var source = await _source.CreateAsync(cancellationToken);
            await using var output = await _output.CreateAsync(cancellationToken);

            await PreProcessAsync(context, cancellationToken);

            await source.LoadAsync(context.Get(_target.EntityType.ClrType), cancellationToken);

            await _db.Database.ExecuteSqlRawAsync(ToSql(), cancellationToken);

            await PostProcessAsync(context, cancellationToken);
        }

        protected virtual Task PreProcessAsync(MergeContext context, CancellationToken cancellationToken = default)
        {
            var entities = context.Get(_target.EntityType.ClrType);
            var navigations = _target.EntityType.GetNavigations().Where(navigation => navigation.IsDependentToPrincipal() && !navigation.DeclaringEntityType.IsOwned()).ToList();

            foreach (var entity in entities)
            foreach (var navigation in navigations)
            {
                var value = navigation.GetGetter().GetClrValue(entity);
                if (value == null)
                    continue;

                var foreignKey = navigation.ForeignKey.PrincipalKey.Properties.Select(property => property.GetGetter().GetClrValue(value)).ToArray();

                for (var index = 0; index < foreignKey.Length; index++)
                    navigation.ForeignKey.Properties[index].PropertyInfo.SetValue(entity, foreignKey[index]);
            }

            return Task.CompletedTask;
        }

        protected virtual async Task PostProcessAsync(MergeContext context, CancellationToken cancellationToken = default)
        {
            var key = _target.EntityType.FindPrimaryKey();
            var ons = _on.Properties.Where(property => !property.IsPrimaryKey()).Concat(key.Properties.Intersect(_on.Properties)).ToList();
            var all = ons.Union(key.Properties).ToList();

            var statement = $"SELECT {string.Join(", ", all.Select(property => $"[{property.GetColumnName()}]"))} FROM [{_output.GetTableName()}] WHERE [{_output.GetActionName()}] IN ('INSERT', 'UPDATE')";
            var connection = (SqlConnection) _db.Database.GetDbConnection();
            var transaction = (SqlTransaction) _db.Database.CurrentTransaction?.GetDbTransaction();

            await using var command = new SqlCommand(statement, connection, transaction);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!reader.HasRows)
                return;

            var entities = context.Get(_target.EntityType.ClrType).Cast<object>().ToLookup(entity => PropertyEqualityComparer.Instance.GetHashCode(ons.Select(property => property.GetGetter().GetClrValue(entity)).ToArray()));
            var navigations = _target.EntityType.GetNavigations().Where(navigation => !navigation.IsDependentToPrincipal() && !navigation.ForeignKey.DeclaringEntityType.IsOwned()).ToList();

            var on = new object[ons.Count];
            var values = new object[all.Count];
            var offset = all.Count - key.Properties.Count;

            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.GetValues(values) != values.Length)
                    throw new InvalidOperationException("Read an incorrect number of columns.");

                for (var index = 0; index < values.Length; index++)
                {
                    var value = values[index];
                    var property = all[index];

                    var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                    if (type.IsEnum)
                        value = Enum.ToObject(type, value);

                    var converter = property.GetValueConverter();
                    if (converter != null)
                        value = converter.ConvertFromProvider(value);

                    values[index] = value;
                }

                Array.Copy(values, on, on.Length);

                var entity = entities[PropertyEqualityComparer.Instance.GetHashCode(on)].SingleOrDefault(obj => PropertyEqualityComparer.Instance.Equals(ons.Select(property => property.GetGetter().GetClrValue(obj)).ToArray(), on));
                if (entity == null)
                    throw new InvalidOperationException("Couldn't find the original entity.");

                for (var index = offset; index < all.Count; index++)
                    all[index].PropertyInfo.SetValue(entity, values[index]);

                foreach (var navigation in navigations)
                {
                    var value = navigation.GetGetter().GetClrValue(entity);
                    if (value == null)
                        continue;

                    var items =
                        navigation.IsCollection()
                            ? (IEnumerable) value
                            : new[] { value };

                    foreach (var item in items)
                    {
                        for (var index = 0; index < navigation.ForeignKey.Properties.Count; index++)
                            navigation.ForeignKey.Properties[index].PropertyInfo.SetValue(item, values[offset + index]);
                    }
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
                                from property in navigation.GetColumns()
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
                command.AppendLine("WHEN MATCHED THEN UPDATE SET @UPDATE = 1");

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
                                from property in navigation.GetColumns()
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

            if (_behavior.HasFlag(MergeBehavior.WhenNotMatchedBySourceThenDelete)) command.AppendLine("WHEN NOT MATCHED BY SOURCE THEN DELETE");

            var output =
                _target
                    .EntityType
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

        private class PropertyEqualityComparer : EqualityComparer<object[]>
        {
            public static PropertyEqualityComparer Instance = new PropertyEqualityComparer();

            private PropertyEqualityComparer()
            {
            }

            public override bool Equals(object[] x, object[] y)
            {
                if (x == null && y == null)
                    return true;
                if (x == null || y == null)
                    return false;
                return x.SequenceEqual(y);
            }

            public override int GetHashCode(object[] obj)
            {
                return obj.Aggregate(0, HashCode.Combine);
            }
        }
    }
}