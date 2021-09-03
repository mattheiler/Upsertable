using System.Threading;
using System.Threading.Tasks;

namespace Marvolo.EntityFramework.SqlMerge
{
    public interface IMerge
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}