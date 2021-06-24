﻿using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.Data.SqlClient;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlBulkCopy
{
    public class SqlBulkCopyMergeSourceLoadStrategy : IMergeSourceLoadStrategy
    {
        private readonly SqlBulkCopyMergeSourceLoadOptions _options;

        public SqlBulkCopyMergeSourceLoadStrategy()
            : this(new SqlBulkCopyMergeSourceLoadOptions())
        {
        }

        public SqlBulkCopyMergeSourceLoadStrategy(SqlBulkCopyMergeSourceLoadOptions options)
        {
            _options = options;
        }

        public async Task ExecuteAsync(IMergeSource source, DataTable table, SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
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