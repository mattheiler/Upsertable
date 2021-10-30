using System.Threading;
using System.Threading.Tasks;

namespace Upsertable.EntityFramework.Abstractions
{
    public interface IMerge
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}