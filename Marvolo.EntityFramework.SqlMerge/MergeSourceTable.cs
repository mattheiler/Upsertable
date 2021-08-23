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
        private readonly IMergeResolver _resolver;

        public MergeSourceTable(MergeSource source, IMergeResolver resolver, IMergeSourceBuilder builder, IMergeSourceLoader loader)
        {
            _source = source;
            _resolver = resolver;
            _builder = builder;
            _loader = loader;
        }

        public ValueTask DisposeAsync()
        {
            return _source.DropTableAsync();
        }

        public async Task LoadAsync(MergeContext context, SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken = default)
        {
            await _loader.ExecuteAsync(_source, _builder.GetDataTable(_source, _resolver.Resolve(context)), connection, transaction, cancellationToken);
        }
    }
}