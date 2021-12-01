using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Upsertable.Abstractions;
using Upsertable.Data;

namespace Upsertable
{
    public abstract class Merge : IMerge
    {
        protected readonly DbContext Db;
        protected readonly EntityProviderFunc EntityProvider;
        protected readonly IMergeOutput Output;
        protected readonly IMergeSource Source;
        protected readonly IEntityType Target;

        protected Merge(DbContext db, IEntityType target, IMergeSource source, IMergeOutput output, EntityProviderFunc entityProvider)
        {
            Db = db;
            Target = target;
            Source = source;
            Output = output;
            EntityProvider = entityProvider;
        }

        public MergeBehavior Behavior { get; set; }

        public List<IProperty> On { get; set; } = new();

        public List<IPropertyBase> Insert { get; } = new();

        public List<IPropertyBase> Update { get; } = new();

        public List<INavigation> Dependents { get; } = new();

        public List<INavigation> Principals { get; } = new();

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var connection = Db.Database.GetDbConnection();
            var transaction = Db.Database.CurrentTransaction?.GetDbTransaction();
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken);

            var entities = EntityProvider().Cast<object>().ToList();

            await PreProcessAsync(entities, connection, transaction, cancellationToken);

            await using var source = await Source.CreateTableAsync(cancellationToken);
            await using var output = await Output.CreateTableAsync(cancellationToken);

            await source.LoadAsync(entities, connection, transaction, cancellationToken);

            await ProcessAsync(cancellationToken);

            await PostProcessAsync(entities, connection, transaction, cancellationToken);
        }

        protected virtual Task PreProcessAsync(IEnumerable<object> entities, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        protected abstract Task ProcessAsync(CancellationToken cancellationToken = default);

        protected virtual Task PostProcessAsync(IEnumerable<object> entities, DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}