using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Marvolo.EntityFramework.SqlMerge
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

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            foreach (var merge in _merges) await merge.ExecuteAsync(cancellationToken);
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, _merges.Select(merge => merge.ToString()));
        }
    }
}