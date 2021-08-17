﻿using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Marvolo.EntityFramework.SqlMerge.SqlBulkCopy
{
    public class SqlBulkCopyMergeSourceLoader : IMergeSourceLoader
    {
        private readonly SqlBulkCopyMergeSourceLoaderOptions _options;

        public SqlBulkCopyMergeSourceLoader(SqlBulkCopyMergeSourceLoaderOptions options)
        {
            _options = options;
        }

        public async Task ExecuteAsync(MergeSource source, DataTable table, SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken = default)
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