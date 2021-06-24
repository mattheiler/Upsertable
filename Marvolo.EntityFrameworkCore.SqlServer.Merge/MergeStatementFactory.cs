using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeStatementFactory
    {
        public MergeStatement CreateStatement(IMerge merge, IEntityType type)
        {
            var command = new StringBuilder();

            command
               .AppendLine("DECLARE @UPDATE bit")
               .AppendLine(";");

            command
               .AppendLine($"MERGE [{type.GetTableName()}] AS [T]")
               .AppendLine($"USING [{merge.Source.GetTableName()}] AS [S]");

            var conditions =
                merge
                   .On
                   .Properties
                   .Select(property => property.GetColumnName())
                   .Select(column => $"[T].[{column}] = [S].[{column}]");

            command.AppendLine($"ON {string.Join(" AND ", conditions)}");

            if (merge.Behavior.HasFlag(MergeBehavior.WhenMatchedThenUpdate))
            {
                var columns = new List<string>();

                foreach (var update in merge.Update.Properties)
                    switch (update)
                    {
                        case IProperty property:
                            if (!property.ValueGenerated.HasFlag(ValueGenerated.OnUpdate) &&
                                property.GetValueGenerationStrategy() != SqlServerValueGenerationStrategy.IdentityColumn)
                                columns.Add(property.GetColumnName());
                            break;
                        case INavigation navigation:
                            var properties =
                                from property in navigation.GetColumns()
                                where !property.ValueGenerated.HasFlag(ValueGenerated.OnUpdate)
                                where property.GetValueGenerationStrategy() != SqlServerValueGenerationStrategy.IdentityColumn
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

            if (merge.Behavior.HasFlag(MergeBehavior.WhenNotMatchedByTargetThenInsert))
            {
                var columns = new List<string>();

                foreach (var insert in merge.Insert.Properties)
                    switch (insert)
                    {
                        case IProperty property:
                            if (!property.ValueGenerated.HasFlag(ValueGenerated.OnAdd) &&
                                property.GetValueGenerationStrategy() != SqlServerValueGenerationStrategy.IdentityColumn)
                                columns.Add(property.GetColumnName());
                            break;
                        case INavigation navigation:
                            var properties =
                                from property in navigation.GetColumns()
                                where !property.ValueGenerated.HasFlag(ValueGenerated.OnAdd)
                                where property.GetValueGenerationStrategy() != SqlServerValueGenerationStrategy.IdentityColumn
                                select property;
                            columns.AddRange(properties.Select(property => property.GetColumnName()));
                            break;
                        default:
                            throw new NotSupportedException("Property or navigation type not supported.");
                    }

                command.AppendLine($"WHEN NOT MATCHED BY TARGET THEN INSERT ({string.Join(", ", columns)}) VALUES ({string.Join(", ", columns.Select(column => $"[S].[{column}]"))})");
            }

            if (merge.Behavior.HasFlag(MergeBehavior.WhenNotMatchedBySourceThenDelete)) command.AppendLine("WHEN NOT MATCHED BY SOURCE THEN DELETE");

            if (merge.Output.Properties.Any())
            {
                var columns =
                    type
                       .GetProperties()
                       .Select(property => property.GetColumnName())
                       .Select(column => $"ISNULL(DELETED.[{column}], INSERTED.[{column}]) AS [{column}]")
                       .Append($"$action AS [{merge.Output.GetActionName()}]");

                command.AppendLine($"OUTPUT {string.Join(", ", columns)} INTO [{merge.Output.GetTableName()}]");
            }

            command.AppendLine(";");

            return new MergeStatement(command.ToString());
        }
    }
}