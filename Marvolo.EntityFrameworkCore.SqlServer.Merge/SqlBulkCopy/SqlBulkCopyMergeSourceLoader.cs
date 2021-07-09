using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlBulkCopy
{
    public class SqlBulkCopyMergeSourceLoader : IMergeSourceLoader
    {
        private readonly SqlBulkCopyMergeSourceLoaderOptions _options;

        public SqlBulkCopyMergeSourceLoader(IOptions<SqlBulkCopyMergeSourceLoaderOptions> options)
        {
            _options = options.Value;
        }

        public async Task ExecuteAsync(MergeSource source, DataTable table, SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
        {
            using var copy = new Microsoft.Data.SqlClient.SqlBulkCopy(connection, default, transaction)
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