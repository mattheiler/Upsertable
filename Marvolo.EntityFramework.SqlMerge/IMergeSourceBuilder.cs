using System.Collections;
using System.Data;

namespace Marvolo.EntityFramework.SqlMerge
{
    public interface IMergeSourceBuilder
    {
        DataTable GetDataTable(MergeSource source, IEnumerable entities);
    }
}