using System.Collections;
using System.Data;

namespace Upsertable.EntityFramework.Abstractions
{
    public interface IDataTableFactory
    {
        DataTable CreateDataTable(IMergeSource source, IEnumerable entities);
    }
}