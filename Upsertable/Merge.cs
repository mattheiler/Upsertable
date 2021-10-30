using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Upsertable.EntityFramework.Abstractions;
using Upsertable.EntityFramework.Data;

namespace Upsertable.EntityFramework
{
    public abstract class Merge : IMerge
    {
        protected readonly DbContext Db;
        protected readonly IEntityType Target;
        protected readonly IMergeSource Source;
        protected readonly IReadOnlyCollection<IProperty> On;
        protected readonly MergeBehavior Behavior;
        protected readonly IReadOnlyCollection<IPropertyBase> Insert;
        protected readonly IReadOnlyCollection<IPropertyBase> Update;
        protected readonly IMergeOutput Output;
        protected readonly EntityProviderFunc EntityProvider;
        protected readonly IReadOnlyCollection<INavigation> Principals;
        protected readonly IReadOnlyCollection<INavigation> Dependents;

        protected Merge
        (
            DbContext db,
            IEntityType target,
            IMergeSource source,
            IReadOnlyCollection<IProperty> on,
            MergeBehavior behavior,
            IReadOnlyCollection<IPropertyBase> insert,
            IReadOnlyCollection<IPropertyBase> update,
            IMergeOutput output,
            EntityProviderFunc entityProvider,
            IEnumerable<INavigation> principals,
            IEnumerable<INavigation> dependents
        )
        {
            Db = db;
            Target = target;
            Source = source;
            On = on;
            Behavior = behavior;
            Insert = insert;
            Update = update;
            Output = output;
            EntityProvider = entityProvider;
            Principals = principals.ToList().AsReadOnly();
            Dependents = dependents.ToList().AsReadOnly();
        }

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