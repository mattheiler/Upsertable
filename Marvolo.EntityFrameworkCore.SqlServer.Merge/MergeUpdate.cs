using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    internal class MergeUpdate : IMergeUpdate
    {
        private MergeUpdate(IEnumerable<IPropertyBase> properties)
        {
            Properties = properties.ToList().AsReadOnly();
        }

        public IReadOnlyList<IPropertyBase> Properties { get; }

        public static IMergeUpdate Select(IEntityType type, LambdaExpression lambda)
        {
            return new MergeUpdate(type.GetColumns(lambda));
        }

        public static IMergeUpdate SelectAll(IEntityType type)
        {
            return new MergeUpdate(type.GetProperties());
        }
    }
}