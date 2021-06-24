using System;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    [Flags]
    public enum MergeBehavior
    {
        WhenMatchedThenUpdate = 1,
        WhenNotMatchedByTargetThenInsert = 2,
        WhenNotMatchedBySourceThenDelete = 4
    }
}