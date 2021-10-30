using System;

namespace Upsertable
{
    [Flags]
    public enum MergeBehavior
    {
        Update = 1,
        Insert = 2
    }
}