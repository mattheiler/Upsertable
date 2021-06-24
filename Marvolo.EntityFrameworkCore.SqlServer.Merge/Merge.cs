using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public sealed class Merge : IMerge
    {
        public Merge(IEntityType target, IMergeSource source, IMergeOn on, MergeBehavior behavior, IMergeInsert insert, IMergeUpdate update, IMergeOutput output, MergeContext context)
        {
            Target = target;
            Source = source;
            On = on;
            Behavior = behavior;
            Insert = insert;
            Update = update;
            Output = output;
            Context = context;
        }

        public IEntityType Target { get; }

        public IMergeSource Source { get; }

        public IMergeOn On { get; }

        public MergeBehavior Behavior { get; }

        public IMergeInsert Insert { get; }

        public IMergeUpdate Update { get; }

        public IMergeOutput Output { get; }

        public MergeContext Context { get; }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var connection = Context.Db.Database.GetDbConnection();
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken);

            await using var source = await Source.CreateAsync(cancellationToken);
            await using var output = await Output.CreateAsync(cancellationToken);

            await source.LoadAsync(Context.Get(Target.ClrType), cancellationToken);

            var command = ToString();

            await Context.Db.Database.ExecuteSqlRawAsync(command, cancellationToken);
        }

        public MergeStatement GetStatement()
        {
            var statementFactory = new MergeStatementFactory();
            var statement = statementFactory.CreateStatement(this, Target);
            return statement;
        }

        public override string ToString()
        {
            return GetStatement().ToString();
        }
    }
}