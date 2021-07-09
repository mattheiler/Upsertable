using System;
using System.Threading;
using System.Threading.Tasks;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeSource
    {
        private readonly string _table = "#SOURCE_" + Guid.NewGuid().ToString().Replace('-', '_');

        public MergeSource(DbContext context, IEntityType entityType, IMergeSourceLoader loader)
        {
            Context = context;
            EntityType = entityType;
            Loader = loader;
        }

        public DbContext Context { get; }

        public IMergeSourceLoader Loader { get; }

        public IEntityType EntityType { get; }

        public async Task<MergeSourceTable> CreateAsync(CancellationToken cancellationToken = default)
        {
            var table = new MergeSourceTable(this, Loader);
            await table.CreateAsync(cancellationToken);
            return table;
        }

        public string GetTableName()
        {
            return _table;
        }
    }
}