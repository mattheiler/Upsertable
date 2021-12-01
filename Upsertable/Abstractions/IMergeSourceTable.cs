using System;
using System.Collections;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Upsertable.Abstractions
{
    public interface IMergeSourceTable : IAsyncDisposable
    {
        Task LoadAsync(IEnumerable entities, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default);
    }
}