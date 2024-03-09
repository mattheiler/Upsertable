using System.Threading;
using System.Threading.Tasks;

namespace Upsertable;

public interface IMerge
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}