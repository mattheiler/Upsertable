using System.Threading;
using System.Threading.Tasks;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions
{
    public interface IMerge
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}