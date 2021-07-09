using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions
{
    public interface IMergeOn
    {
        IReadOnlyList<IProperty> Properties { get; }
    }
}