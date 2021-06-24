using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    internal class MergeInsert : IMergeInsert
    {
        private MergeInsert(IEnumerable<IPropertyBase> properties)
        {
            Properties = properties.ToList().AsReadOnly();
        }

        public IReadOnlyList<IPropertyBase> Properties { get; }


        public static IMergeInsert Select(IEntityType type, LambdaExpression lambda)
        {
            return new MergeInsert(type.GetColumns(lambda));
        }

        public static IMergeInsert SelectAll(IEntityType type)
        {
            return new MergeInsert(type.GetColumns());
        }
    }
}