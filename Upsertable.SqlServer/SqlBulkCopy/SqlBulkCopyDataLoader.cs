using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Upsertable.SqlServer.SqlBulkCopy;

public class SqlBulkCopyDataLoader(SqlBulkCopyDataLoaderOptions options) : IDataLoader
{
    public async Task LoadAsync(Source source, DataTable table, DbConnection connection, DbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        using var copy = new Microsoft.Data.SqlClient.SqlBulkCopy((SqlConnection)connection, default, (SqlTransaction?)transaction)
        {
            BatchSize = options.BatchSize,
            DestinationTableName = source.GetTableName(),
            EnableStreaming = true,
            BulkCopyTimeout = options.BulkCopyTimeout
        };

        await copy.WriteToServerAsync(table, cancellationToken);
    }
}