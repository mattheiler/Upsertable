using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Upsertable.SqlServer.SqlBulkCopy;

public class SqlBulkCopyDataLoader : IDataLoader
{
    private readonly SqlBulkCopyDataLoaderOptions _options;

    public SqlBulkCopyDataLoader(SqlBulkCopyDataLoaderOptions options)
    {
        _options = options;
    }

    public async Task LoadAsync(SqlServerMergeSource source, DataTable table, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
    {
        using var copy = new Microsoft.Data.SqlClient.SqlBulkCopy((SqlConnection)connection, default, (SqlTransaction)transaction)
        {
            BatchSize = _options.BatchSize,
            DestinationTableName = source.GetTableName(),
            EnableStreaming = true,
            BulkCopyTimeout = _options.BulkCopyTimeout
        };

        await copy.WriteToServerAsync(table, cancellationToken);
    }
}