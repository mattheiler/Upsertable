using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions
{
    public interface IMergeSourceLoader
    {
        Task ExecuteAsync(IMergeSource source, DataTable table, SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken = default);
    }
}