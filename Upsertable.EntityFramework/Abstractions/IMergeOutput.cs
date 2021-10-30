using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Upsertable.EntityFramework.Abstractions
{
    public interface IMergeOutput
    {
        Task<IMergeOutputTable> CreateTableAsync(CancellationToken cancellationToken);

        Task DropTableAsync();

        string GetTableName();

        IEnumerable<IProperty> GetProperties();
    }
}