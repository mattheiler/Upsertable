using System.Collections;
using System.Data;

namespace Upsertable.SqlServer;

public interface IDataTableFactory
{
    DataTable CreateDataTable(SqlServerMergeSource source, IEnumerable entities);
}