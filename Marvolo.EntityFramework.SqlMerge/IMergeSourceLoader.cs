using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Marvolo.EntityFramework.SqlMerge
{
    public interface IMergeSourceLoader
    {
        Task ExecuteAsync(MergeSource source, DataTable table, SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken = default);
    }
}