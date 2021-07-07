using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions
{
    public interface IMergeTarget
    {
        IEntityType EntityType { get; }
        string GetTableName();
    }
}