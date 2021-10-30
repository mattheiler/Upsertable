using System.Collections;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Upsertable.Abstractions;

namespace Upsertable.SqlServer
{
    public class SqlServerMergeSourceTable : IMergeSourceTable
    {
        private readonly IMergeSource _source;
        private readonly IDataTableFactory _tableResolver;
        private readonly IDataTableLoader _tableLoader;

        public SqlServerMergeSourceTable(IMergeSource source, IDataTableFactory tableResolver, IDataTableLoader tableLoader)
        {
            _source = source;
            _tableResolver = tableResolver;
            _tableLoader = tableLoader;
        }

        public async ValueTask DisposeAsync()
        {
            await _source.DropTableAsync();
        }

        public async Task LoadAsync(IEnumerable entities, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
        {
            await _tableLoader.LoadAsync(_source, _tableResolver.CreateDataTable(_source, entities), connection, transaction, cancellationToken);
        }
    }
}