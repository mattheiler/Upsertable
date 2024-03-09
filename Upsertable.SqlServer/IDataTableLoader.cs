using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Upsertable.SqlServer;

public interface IDataTableLoader
{
    Task LoadAsync(SqlServerMergeSource source, DataTable table, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
}