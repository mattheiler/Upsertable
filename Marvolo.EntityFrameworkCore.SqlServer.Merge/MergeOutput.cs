using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeOutput : IMergeOutput
    {
        private readonly string _action = "__ACTION__";
        private readonly string _table = "#OUTPUT_" + Guid.NewGuid().ToString().Replace('-', '_');

        public MergeOutput(DbContext context, IEntityType entityType, IEnumerable<IPropertyBase> properties)
        {
            Context = context;
            EntityType = entityType;
            Properties = properties.ToList();
        }

        public DbContext Context { get; }

        public IEntityType EntityType { get; }

        public IReadOnlyList<IPropertyBase> Properties { get; }

        public string GetTableName()
        {
            return _table;
        }

        public string GetActionName()
        {
            return _action;
        }

        public async Task<IMergeOutputTable> CreateAsync(CancellationToken cancellationToken = default)
        {
            var table = new MergeOutputTable(this);
            await table.CreateAsync(cancellationToken);
            return table;
        }
    }
}