using System;
using System.Threading.Tasks;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeOutputTable : IAsyncDisposable
    {
        private readonly MergeOutput _output;

        public MergeOutputTable(MergeOutput output)
        {
            _output = output;
        }

        public async ValueTask DisposeAsync()
        {
            await _output.DropTableAsync();
        }
    }
}