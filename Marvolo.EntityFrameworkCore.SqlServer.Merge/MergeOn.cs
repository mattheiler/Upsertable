using System.Collections.Generic;
using System.Linq;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    internal class MergeOn : IMergeOn
    {
        public MergeOn(IEnumerable<IProperty> properties)
        {
            Properties = properties.ToList().AsReadOnly();
        }

        public IReadOnlyList<IProperty> Properties { get; }
    }
}