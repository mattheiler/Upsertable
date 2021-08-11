using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFramework.SqlMerge
{
    public class MergeInsert
    {
        public MergeInsert(IEnumerable<IPropertyBase> properties)
        {
            Properties = properties.ToList().AsReadOnly();
        }

        public IReadOnlyList<IPropertyBase> Properties { get; }
    }
}