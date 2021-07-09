using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public static class MergeBuilderExtensions
    {
        public static MergeBuilder<T> Merge<T>(this DbContext @this, IEnumerable<T> entities)
            where T : class
        {
            var context = new MergeContext(@this);
            context.AddRange(entities);
            return new MergeBuilder<T>(context);
        }
    }
}