namespace Marvolo.EntityFramework.SqlMerge.SqlBulkCopy
{
    public class SqlBulkCopyMergeSourceLoaderOptions
    {
        public int BulkCopyTimeout { get; set; } = 30;

        public int BatchSize { get; set; }
    }
}