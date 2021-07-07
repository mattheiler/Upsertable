using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeTarget : IMergeTarget
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