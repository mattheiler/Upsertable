using System.Collections;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFramework.SqlMerge
{
    public class EntityTypeMergeResolver : IMergeResolver
    {
        private readonly IEntityType _entityType;

        public EntityTypeMergeResolver(IEntityType entityType)
        {
            _entityType = entityType;
        }

        public IEnumerable Resolve(MergeContext context)
        {
            return context.Get(_entityType.ClrType);
        }
    }
}