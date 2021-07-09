using System;
using System.Collections.Generic;
using System.Linq;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    internal class MergeComparer : EqualityComparer<object[]>
    {
        public override bool Equals(object[] x, object[] y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return x.SequenceEqual(y);
        }

        public override int GetHashCode(object[] obj)
        {
            return obj.Aggregate(0, HashCode.Combine);
        }
    }
}