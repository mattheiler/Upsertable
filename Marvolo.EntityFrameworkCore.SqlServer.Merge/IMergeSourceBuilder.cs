using System.Collections;
using System.Data;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public interface IMergeSourceBuilder
    {
        DataTable GetDataTable(MergeSource source, IEnumerable entities);
    }
}