using System.Threading.Tasks;
using Upsertable.Abstractions;

namespace Upsertable.SqlServer
{
    public class SqlServerMergeOutputTable : IMergeOutputTable
    {
        private readonly SqlServerMergeOutput _output;

        public SqlServerMergeOutputTable(SqlServerMergeOutput output)
        {
            _output = output;
        }

        public async ValueTask DisposeAsync()
        {
            await _output.DropTableAsync();
        }
    }
}