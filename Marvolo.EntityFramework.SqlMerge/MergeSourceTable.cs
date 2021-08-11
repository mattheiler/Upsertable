using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Marvolo.EntityFramework.SqlMerge
{
    public class MergeSourceTable : IAsyncDisposable
    {
        private readonly IMergeSourceBuilder _builder;
        private readonly IMergeSourceLoader _loader;
        private readonly MergeSource _source;

        public MergeSourceTable(MergeSource source, IMergeSourceBuilder builder, IMergeSourceLoader loader)
        {
            _source = source;
            _builder = builder;
            _loader = loader;
        }

        public ValueTask DisposeAsync()
        {
            return _source.DropTableAsync();
        }

        public async Task LoadAsync(IEnumerable entities, SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken = default)
        {
            await _loader.ExecuteAsync(_source, _builder.GetDataTable(_source, entities), connection, transaction, cancellationToken);
        }
    }
}