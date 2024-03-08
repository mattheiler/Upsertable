using System.Collections;
using System.Data;
using Upsertable.SqlServer;

namespace Upsertable.Abstractions;

public interface IDataTableFactory
{
    DataTable CreateDataTable(SqlServerMergeSource source, IEnumerable entities);
}