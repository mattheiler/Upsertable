﻿namespace Upsertable.SqlBulkCopy;

public class SqlBulkCopyDataLoaderOptions
{
    public int BulkCopyTimeout { get; set; } = 30;

    public int BatchSize { get; set; }
}