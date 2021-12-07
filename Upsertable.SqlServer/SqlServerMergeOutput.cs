using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Upsertable.Abstractions;
using Upsertable.Extensions;

namespace Upsertable.SqlServer
{
    public class SqlServerMergeOutput : IMergeOutput
    {
        private readonly DbContext _db;
        private readonly IList<IProperty> _properties;
        private readonly string _table = "#OUTPUT_" + Guid.NewGuid().ToString().Replace('-', '_');

        public SqlServerMergeOutput(DbContext db, IEnumerable<IProperty> properties)
        {
            _db = db;
            _properties = properties.ToList();
        }

        public async Task<IMergeOutputTable> CreateTableAsync(CancellationToken cancellationToken = default)
        {
            var columns = GetProperties().Select(property => property.GetColumnName(StoreObjectIdentifier.Table(property.DeclaringEntityType.GetTableName(), property.DeclaringEntityType.GetSchema())));
            var definitions = GetProperties().Select(property => $"{property.GetColumnNameInTable()} {property.GetColumnType()}");
            var command = $"CREATE TABLE [{GetTableName()}] ({string.Join(", ", definitions)})";

            await _db.Database.ExecuteSqlRawAsync(command, cancellationToken);

            return new SqlServerMergeOutputTable(this);
        }

        public async Task DropTableAsync()
        {
            await _db.Database.ExecuteSqlRawAsync($"DROP TABLE {GetTableName()}");
        }

        public IEnumerable<IProperty> GetProperties()
        {
            return _properties;
        }

        public string GetTableName()
        {
            return _table;
        }
    }
}