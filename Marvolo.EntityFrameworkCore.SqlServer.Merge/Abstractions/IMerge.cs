using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions
{
    public interface IMerge
    {
        public MergeContext Context { get; }

        public MergeBehavior Behavior { get; }

        public IMergeOn On { get; }

        public IEntityType Target { get; }

        public IMergeSource Source { get; }

        public IMergeUpdate Update { get; }

        public IMergeInsert Insert { get; }

        public IMergeOutput Output { get; }

        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}