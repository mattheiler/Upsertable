using System.Threading;
using System.Threading.Tasks;

namespace Upsertable.Abstractions
{
    public interface IMerge
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}