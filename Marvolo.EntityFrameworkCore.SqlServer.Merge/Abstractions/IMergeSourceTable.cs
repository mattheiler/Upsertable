using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions
{
    public interface IMergeSourceTable : IAsyncDisposable
    {
        Task LoadAsync(IEnumerable entities, CancellationToken cancellationToken = default);
    }
}