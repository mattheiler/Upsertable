using System.Threading;
using System.Threading.Tasks;

namespace Upsertable.SqlServer;

public interface IMerge
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}