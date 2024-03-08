using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Upsertable.SqlServer;

namespace Upsertable.Abstractions;

public interface IDataTableLoader
{
    Task LoadAsync(SqlServerMergeSource source, DataTable table, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
}