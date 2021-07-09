namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlBulkCopy
{
    public class SqlBulkCopyMergeSourceLoaderOptions
    {
        public int BulkCopyTimeout { get; set; } = 30;

        public int BatchSize { get; set; }
    }
}