using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions
{
    public interface IMergeInsert
    {
        IReadOnlyList<IPropertyBase> Properties { get; }
    }
}