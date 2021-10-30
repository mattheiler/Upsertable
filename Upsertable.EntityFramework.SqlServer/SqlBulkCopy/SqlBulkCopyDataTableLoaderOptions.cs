namespace Upsertable.EntityFramework.SqlServer.SqlBulkCopy
{
    public class SqlBulkCopyDataTableLoaderOptions
    {
        public int BulkCopyTimeout { get; set; } = 30;

        public int BatchSize { get; set; }
    }
}