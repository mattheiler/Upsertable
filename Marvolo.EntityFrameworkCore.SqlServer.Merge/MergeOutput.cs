using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeOutput
    {
        private readonly DbContext _db;
        private readonly IList<IProperty> _properties;
        private readonly string _table = "#OUTPUT_" + Guid.NewGuid().ToString().Replace('-', '_');

        public MergeOutput(DbContext db, IEnumerable<IProperty> properties)
        {
            _db = db;
            _properties = properties.ToList();
        }

        public async Task<MergeOutputTable> CreateTableAsync(CancellationToken cancellationToken = default)
        {
            var definitions = _properties.Select(property => $"{property.GetColumnName()} {property.GetColumnType()}");
            var command = $"CREATE TABLE [{GetTableName()}] ({string.Join(", ", definitions)})";

            await _db.Database.ExecuteSqlRawAsync(command, cancellationToken);

            return new MergeOutputTable(this);
        }

        internal async Task DropTableAsync()
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