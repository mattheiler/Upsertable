using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions
{
    public interface IMergeSource
    {
        IEntityType EntityType { get; }

        string GetTableName();

        Task<IMergeSourceTable> CreateAsync(CancellationToken cancellationToken = default);
    }
}