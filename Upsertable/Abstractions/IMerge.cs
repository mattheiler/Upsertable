using System.Threading;
using System.Threading.Tasks;

namespace Upsertable.Abstractions;

public interface IMerge
{
    public MergeBehavior Behavior { get; }

    Task ExecuteAsync(CancellationToken cancellationToken = default);
}