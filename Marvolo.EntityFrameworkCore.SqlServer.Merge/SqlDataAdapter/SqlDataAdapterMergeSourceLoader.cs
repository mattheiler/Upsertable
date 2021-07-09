using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlDataAdapter
{
    public class SqlDataAdapterMergeSourceLoader : IMergeSourceLoader
    {
        private readonly SqlDataAdapterMergeSourceLoaderOptions _options;

        public SqlDataAdapterMergeSourceLoader(IOptions<SqlDataAdapterMergeSourceLoaderOptions> options)
        {
            _options = options.Value;
        }

        public async Task ExecuteAsync(MergeSource source, DataTable table, SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
        {
            var command = new SqlCommand
            {
                Connection = connection,
                Transaction = transaction,
                CommandTimeout = _options.CommandTimeout
            };

            foreach (var column in source.EntityType.GetColumns())
            {
                switch (column)
                {
                    case IProperty property:
                        command.Parameters.Add(GetParameter(command, property));
                        break;
                    case INavigation navigation:
                        command.Parameters.AddRange(navigation.GetColumns().Select(property => GetParameter(command, property)).ToArray());
                        break;
                    default:
                        throw new NotSupportedException("Property or navigation type not supported.");
                }
            }

            var columns = command.Parameters.OfType<DbParameter>().Select(parameter => $"[{parameter.SourceColumn}]");
            var parameters = command.Parameters.OfType<DbParameter>().Select(parameter => parameter.ParameterName);

            command.CommandText = $"INSERT INTO [{source.GetTableName()}] ({string.Join(',', columns)}) VALUES ({string.Join(',', parameters)})";

            var adapter = new Microsoft.Data.SqlClient.SqlDataAdapter { InsertCommand = command };

            await Task.Yield();

            adapter.Update(table);
        }

        private static DbParameter GetParameter(DbCommand command, IProperty property)
        {
            var parameter = property.GetRelationalTypeMapping().CreateParameter(command, $"@{property.GetColumnName()}", default);

            parameter.SourceColumn = property.GetColumnName();
            parameter.Value = default;

            return parameter;
        }
    }
}