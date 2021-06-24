using System;
using System.Collections;
using System.Threading.Tasks;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions
{
    public interface IMergeOutputTable : IAsyncDisposable
    {
        Task<IEnumerable> GetAsync(params string[] actions);
    }
}