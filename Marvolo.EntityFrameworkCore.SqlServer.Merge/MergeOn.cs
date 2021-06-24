using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    internal class MergeOn : IMergeOn
    {
        private MergeOn(IEnumerable<IProperty> properties, IEqualityComparer<object[]> comparer)
        {
            Properties = properties.OrderBy(property => property.IsPrimaryKey()).ToList().AsReadOnly();
            Comparer = comparer;
        }

        public IEqualityComparer<object[]> Comparer { get; }

        public IReadOnlyList<IProperty> Properties { get; }

        public static IMergeOn Select(IEntityType type, LambdaExpression lambda)
        {
            return new MergeOn(type.GetColumns(lambda).Cast<IProperty>(), MergeOnEqualityComparer.Instance);
        }

        public static IMergeOn SelectPrimaryKeys(IEntityType type)
        {
            return new MergeOn(type.GetProperties().Where(property => property.IsPrimaryKey()), MergeOnEqualityComparer.Instance);
        }
    }
}