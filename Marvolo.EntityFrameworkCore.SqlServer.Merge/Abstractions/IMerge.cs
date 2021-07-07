using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions
{
    public interface IMerge
    {
        public MergeContext Context { get; }

        public MergeBehavior Behavior { get; }

        public IMergeOn On { get; } // properties & comparer... but the comparer should never change, although...

        public IMergeTarget Target { get; }

        public IMergeSource Source { get; } // entity type, table name, and create table

        public IMergeUpdate Update { get; } // properties

        public IMergeInsert Insert { get; } // properties

        public IMergeOutput Output { get; } // entity type, table name, and create table... properties and action name

        Task ExecuteAsync(CancellationToken cancellationToken = default); // table dispose scope is broken
    }
}