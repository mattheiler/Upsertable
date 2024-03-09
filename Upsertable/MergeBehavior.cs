using System;

namespace Upsertable.SqlServer;

[Flags]
public enum MergeBehavior
{
    Update = 1,
    Insert = 2
}