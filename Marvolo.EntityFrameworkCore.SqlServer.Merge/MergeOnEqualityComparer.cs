using System;
using System.Collections.Generic;
using System.Linq;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeOnEqualityComparer : EqualityComparer<IEnumerable<object>>
    {
        public new static readonly MergeOnEqualityComparer Default = new MergeOnEqualityComparer();

        private MergeOnEqualityComparer()
        {
        }

        public override bool Equals(IEnumerable<object> x, IEnumerable<object> y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return x.SequenceEqual(y);
        }

        public override int GetHashCode(IEnumerable<object> obj)
        {
            return obj.Aggregate(0, HashCode.Combine);
        }
    }
}