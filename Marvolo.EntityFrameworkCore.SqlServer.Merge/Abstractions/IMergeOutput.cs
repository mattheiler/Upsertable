using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions
{
    public interface IMergeOutput
    {
        IEntityType EntityType { get; }

        IReadOnlyList<IPropertyBase> Properties { get; }

        string GetTableName();

        string GetActionName();

        Task<IMergeOutputTable> CreateAsync(CancellationToken cancellationToken = default);
    }
}