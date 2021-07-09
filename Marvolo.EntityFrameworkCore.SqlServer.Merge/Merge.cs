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
        public Merge(MergeTarget target, MergeSource source, MergeOn on, MergeBehavior behavior, MergeInsert insert, MergeUpdate update, MergeOutput output)
        {
            Target = target;
            Source = source;
            On = on;
            Behavior = behavior;
            Insert = insert;
            Update = update;
            Output = output;
        }

        public MergeTarget Target { get; }

        public MergeSource Source { get; }

        public MergeOn On { get; }

        public MergeBehavior Behavior { get; }

        public MergeInsert Insert { get; }

        public MergeUpdate Update { get; }

        public MergeOutput Output { get; }

        public async Task ExecuteAsync(MergeContext context, CancellationToken cancellationToken = default)
        {
            var connection = context.Db.Database.GetDbConnection();
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken);

            await using var source = await Source.CreateAsync(cancellationToken);
            await using var output = await Output.CreateAsync(cancellationToken);

            await source.LoadAsync(context.Get(Target.EntityType.ClrType), cancellationToken);

            await PreProcessAsync(context,cancellationToken);

            await context.Db.Database.ExecuteSqlRawAsync(ToString(), cancellationToken);

            await PostProcessAsync(context, cancellationToken);
        }

        protected virtual Task PreProcessAsync(MergeContext context, CancellationToken cancellationToken = default)
        {
            var entities = context.Get(Target.EntityType.ClrType);
            var navigations = Target.EntityType.GetNavigations().Where(navigation => navigation.IsDependentToPrincipal() && !navigation.DeclaringEntityType.IsOwned()).ToList();

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
            var key = Target.EntityType.FindPrimaryKey();
            var properties = On.Properties.OrderBy(property => property.IsPrimaryKey()).Union(key.Properties).ToList();

            var statement = $"SELECT {string.Join(", ", properties.Select(property => $"[{property.GetColumnName()}]"))} FROM [{Output.GetTableName()}] WHERE [{Output.GetActionName()}] IN ('INSERT', 'UPDATE')";
            var connection = (SqlConnection) context.Db.Database.GetDbConnection();
            var transaction = (SqlTransaction) context.Db.Database.CurrentTransaction?.GetDbTransaction();

            await using var command = new SqlCommand(statement, connection, transaction);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!reader.HasRows)
                return;

            var comparer = new MergeComparer();
            var entities = context.Get(Target.EntityType.ClrType).Cast<object>().ToLookup(entity => comparer.GetHashCode(properties.Select(property => property.GetGetter().GetClrValue(entity)).ToArray()));
            var navigations = Target.EntityType.GetNavigations().Where(navigation => !navigation.IsDependentToPrincipal() && !navigation.ForeignKey.DeclaringEntityType.IsOwned()).ToList();

            var raw = new object[properties.Count];
            var values = new object[properties.Count];
            var offset = properties.Count - key.Properties.Count;
            var on = new object[properties.Count];

            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.GetValues(raw) != raw.Length)
                    throw new InvalidOperationException("Read an incorrect number of columns.");

                for (var index = 0; index < raw.Length; index++)
                {
                    var value = raw[index];
                    var property = properties[index];

                    var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                    if (type.IsEnum)
                        value = Enum.ToObject(type, value);

                    var converter = property.GetValueConverter();
                    if (converter != null)
                        value = converter.ConvertFromProvider(value);

                    values[index] = value;
                }

                Array.Copy(values, on, on.Length);

                

                var entity = entities[comparer.GetHashCode(on)].SingleOrDefault(obj => comparer.Equals(properties.Select(property => property.GetGetter().GetClrValue(obj)).ToArray(), on));
                if (entity == null)
                    throw new InvalidOperationException("Couldn't find the original entity.");

                for (var index = offset; index < properties.Count; index++)
                    properties[index].PropertyInfo.SetValue(entity, values[index]);

                foreach (var navigation in navigations)
                {
                    var value = navigation.GetGetter().GetClrValue(entity);
                    if (value == null)
                        continue;

                    var items =
                        navigation.IsCollection()
                            ? (IEnumerable)value
                            : new[] { value };

                    foreach (var item in items)
                        for (var index = 0; index < properties.Count; index++)
                            properties[offset].PropertyInfo.SetValue(item, values[offset + index]);
                }
            }
        }

        public MergeStatement ToStatement()
        {
            var command = new StringBuilder();

            command
                .AppendLine("DECLARE @UPDATE bit")
                .AppendLine(";");

            command
                .AppendLine($"MERGE [{Target.GetTableName()}] AS [T]")
                .AppendLine($"USING [{Source.GetTableName()}] AS [S]");

            var conditions =
                On
                    .Properties
                    .Select(property => property.GetColumnName())
                    .Select(column => $"[T].[{column}] = [S].[{column}]");

            command.AppendLine($"ON {string.Join(" AND ", conditions)}");

            if (Behavior.HasFlag(MergeBehavior.WhenMatchedThenUpdate))
            {
                var columns = new List<string>();

                foreach (var update in Update.Properties)
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

                command.AppendLine($"WHEN MATCHED THEN UPDATE SET {string.Join(", ", columns.Select(column => $"[T].[{column}] = [S].[{column}]"))}");
            }
            else
            {
                command.AppendLine("WHEN MATCHED THEN UPDATE SET @UPDATE = 1");
            }

            if (Behavior.HasFlag(MergeBehavior.WhenNotMatchedByTargetThenInsert))
            {
                var columns = new List<string>();

                foreach (var insert in Insert.Properties)
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

                command.AppendLine($"WHEN NOT MATCHED BY TARGET THEN INSERT ({string.Join(", ", columns)}) VALUES ({string.Join(", ", columns.Select(column => $"[S].[{column}]"))})");
            }

            if (Behavior.HasFlag(MergeBehavior.WhenNotMatchedBySourceThenDelete))
            {
                command.AppendLine("WHEN NOT MATCHED BY SOURCE THEN DELETE");
            }

            var output =
                Target
                    .EntityType
                    .GetProperties()
                    .Select(property => property.GetColumnName())
                    .Select(column => $"INSERTED.[{column}] AS [{column}]")
                    .Append($"$action AS [{Output.GetActionName()}]");

            command
                .AppendLine($"OUTPUT {string.Join(", ", output)} INTO [{Output.GetTableName()}]")
                .AppendLine(";");

            return new MergeStatement(command.ToString());
        }

        public override string ToString()
        {
            return ToStatement().ToString();
        }
    }
}