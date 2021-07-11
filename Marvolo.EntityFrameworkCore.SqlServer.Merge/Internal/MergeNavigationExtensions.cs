using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Internal
{
    internal static class MergeNavigationExtensions
    {
        public static IEnumerable<IProperty> GetPropertiesWhereIsNotPrimaryKey(this INavigation navigation)
        {
            return
                from property in navigation.DeclaringType.Model.FindEntityType(navigation.ClrType).GetProperties()
                where !property.IsPrimaryKey()
                select property;
        }
    }
}