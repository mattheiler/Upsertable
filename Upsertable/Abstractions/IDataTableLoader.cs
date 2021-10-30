using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Upsertable.Abstractions
{
    public interface IDataTableLoader
    {
        Task LoadAsync(IMergeSource source, DataTable table, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
    }
}