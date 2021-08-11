using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFramework.SqlMerge
{
    public class MergeOn
    {
        public MergeOn(IEnumerable<IProperty> properties)
        {
            Properties = properties.ToList().AsReadOnly();
        }

        public IReadOnlyList<IProperty> Properties { get; }
    }
}