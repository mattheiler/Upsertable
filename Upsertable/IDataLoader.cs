using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Upsertable;

public interface IDataLoader
{
    Task LoadAsync(Source source, DataTable table, DbConnection connection, DbTransaction? transaction, CancellationToken cancellationToken = default);
}