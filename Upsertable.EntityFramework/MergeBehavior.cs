using System;

namespace Upsertable.EntityFramework
{
    [Flags]
    public enum MergeBehavior
    {
        Update = 1,
        Insert = 2
    }
}