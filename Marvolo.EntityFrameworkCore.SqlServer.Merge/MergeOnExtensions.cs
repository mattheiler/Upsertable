using System.Linq;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    internal static class MergeOnExtensions
    {
        public static object[] GetValues(this IMergeOn @this, object entity)
        {
            return @this.Properties.Select(property => property.GetGetter().GetClrValue(entity)).ToArray();
        }
    }
}