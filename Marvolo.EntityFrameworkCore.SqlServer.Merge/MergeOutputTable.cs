using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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
            await DropAsync();
        }

        internal async Task CreateAsync(CancellationToken cancellationToken = default)
        {
            var definitions =
                _output
                    .EntityType
                    .GetProperties()
                    .Select(property => $"{property.GetColumnName()} {property.GetColumnType()}")
                    .Append($"{_output.GetActionName()} nvarchar(10)");

            var command = $"CREATE TABLE [{_output.GetTableName()}] ({string.Join(", ", definitions)})";

            await _output.Context.Database.ExecuteSqlRawAsync(command, cancellationToken);
        }

        private async Task DropAsync()
        {
            await _output.Context.Database.ExecuteSqlRawAsync($"DROP TABLE {_output.GetTableName()}");
        }
    }
}