using System.Threading;
using System.Threading.Tasks;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public interface IMerge
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}