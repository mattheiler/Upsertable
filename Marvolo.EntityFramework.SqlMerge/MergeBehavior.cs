using System;

namespace Marvolo.EntityFramework.SqlMerge
{
    [Flags]
    public enum MergeBehavior
    {
        WhenMatchedThenUpdate = 1,
        WhenNotMatchedByTargetThenInsert = 2,
        WhenNotMatchedBySourceThenDelete = 4
    }
}