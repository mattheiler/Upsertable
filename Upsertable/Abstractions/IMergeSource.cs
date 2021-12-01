using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Upsertable.Abstractions
{
    public interface IMergeSource
    {
        Task<IMergeSourceTable> CreateTableAsync(CancellationToken cancellationToken);

        Task DropTableAsync();

        string GetTableName();

        IEnumerable<IPropertyBase> GetProperties();
    }
}