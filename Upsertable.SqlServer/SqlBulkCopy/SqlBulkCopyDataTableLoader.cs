using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Upsertable.Abstractions;

namespace Upsertable.SqlServer.SqlBulkCopy
{
    public class SqlBulkCopyDataTableLoader : IDataTableLoader
    {
        private readonly SqlBulkCopyDataTableLoaderOptions _options;

        public SqlBulkCopyDataTableLoader(SqlBulkCopyDataTableLoaderOptions options)
        {
            _options = options;
        }

        public async Task LoadAsync(IMergeSource source, DataTable table, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
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
}