using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeOutputTable : IMergeOutputTable
    {
        private readonly MergeOutput _output;

        public MergeOutputTable(MergeOutput output)
        {
            _output = output;
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

        internal async Task DropAsync()
        {
            await _output.Context.Database.ExecuteSqlRawAsync($"DROP TABLE {_output.GetTableName()}");
        }

        public Task<IEnumerable> GetAsync(params string[] actions)
        {
            throw new NotSupportedException();
        }

        public async ValueTask DisposeAsync()
        {
            // TODO move this to a higher context? - the tables kept getting deleted before getting to decorators
            // await DropAsync();
        }
    }
}