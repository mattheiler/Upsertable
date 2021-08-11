using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFramework.SqlMerge
{
    public class MergeTarget
    {
        public MergeTarget(IEntityType entityType)
        {
            EntityType = entityType;
        }

        public IEntityType EntityType { get; }

        public string GetTableName()
        {
            return EntityType.GetTableName();
        }
    }
}