using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeComposite : IMerge
    {
        private readonly List<IMerge> _merges;

        public MergeComposite(IEnumerable<IMerge> merges)
        {
            _merges = merges.ToList();
        }

        public MergeComposite(params IMerge[] merges)
            : this(merges.AsEnumerable())
        {
        }

        public MergeContext Context => throw new NotSupportedException("Unavailable in a composite merge.");

        public MergeBehavior Behavior => throw new NotSupportedException("Unavailable in a composite merge.");

        public IMergeOn On => throw new NotSupportedException("Unavailable in a composite merge.");

        public IMergeInsert Insert => throw new NotSupportedException("Unavailable in a composite merge.");

        public IMergeUpdate Update => throw new NotSupportedException("Unavailable in a composite merge.");

        public IMergeOutput Output => throw new NotSupportedException("Unavailable in a composite merge.");

        public IMergeSource Source => throw new NotSupportedException("Unavailable in a composite merge.");

        public IMergeTarget Target => throw new NotSupportedException("Unavailable in a composite merge.");

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            foreach (var merge in _merges)
                await merge.ExecuteAsync(cancellationToken);
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, _merges.Select(merge => merge.ToString()));
        }
    }
}