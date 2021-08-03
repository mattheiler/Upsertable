using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
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