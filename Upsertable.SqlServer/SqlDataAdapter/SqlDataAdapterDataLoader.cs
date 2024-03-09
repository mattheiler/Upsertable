using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Upsertable.Extensions;

namespace Upsertable.SqlServer.SqlDataAdapter;

public class SqlDataAdapterDataLoader : IDataLoader
{
    private readonly SqlDataAdapterDataLoaderOptions _options;

    public SqlDataAdapterDataLoader(SqlDataAdapterDataLoaderOptions options)
    {
        _options = options;
    }

    public Task LoadAsync(Source source, DataTable table, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var command = new SqlCommand
        {
            Connection = (SqlConnection)connection,
            Transaction = (SqlTransaction)transaction,
            CommandTimeout = _options.CommandTimeout
        };

        foreach (var column in source.GetProperties())
            switch (column)
            {
                case IProperty property:
                    command.Parameters.Add(GetParameter(command, property));
                    break;
                case INavigation navigation:
                    command.Parameters.AddRange(navigation.TargetEntityType.GetProperties().Where(property => !property.IsPrimaryKey()).Select(property => GetParameter(command, property)).ToArray());
                    break;
                default:
                    throw new NotSupportedException("Property or navigation type not supported.");
            }

        var columns = command.Parameters.OfType<DbParameter>().Select(parameter => $"[{parameter.SourceColumn}]");
        var parameters = command.Parameters.OfType<DbParameter>().Select(parameter => parameter.ParameterName);

        command.CommandText = $"INSERT INTO [{source.GetTableName()}] ({string.Join(',', columns)}) VALUES ({string.Join(',', parameters)})";

        var adapter = new Microsoft.Data.SqlClient.SqlDataAdapter { InsertCommand = command };

        adapter.Update(table);

        return Task.CompletedTask;
    }

    private static DbParameter GetParameter(DbCommand command, IProperty property)
    {
        var parameter = property.GetRelationalTypeMapping().CreateParameter(command, $"@{property.GetColumnNameInTable()}", null);

        parameter.SourceColumn = property.GetColumnNameInTable();
        parameter.Value = null;

        return parameter;
    }
}