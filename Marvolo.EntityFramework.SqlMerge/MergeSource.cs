using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFramework.SqlMerge
{
    public class MergeSource
    {
        private readonly IMergeSourceBuilder _builder;
        private readonly DbContext _db;
        private readonly IEntityResolver _resolver;
        private readonly IMergeSourceLoader _loader;
        private readonly IList<IPropertyBase> _properties;
        private readonly string _table = "#SOURCE_" + Guid.NewGuid().ToString().Replace('-', '_');

        public MergeSource(DbContext db, IEnumerable<IPropertyBase> properties, IMergeSourceBuilder builder, IMergeSourceLoader loader)
        {
            _db = db;
            _properties = properties.ToList();
            _builder = builder;
            _loader = loader;
        }

        public async Task<MergeSourceTable> CreateTableAsync(CancellationToken cancellationToken = default)
        {
            var columns = new List<string>();

            foreach (var column in GetProperties())
            {
                switch (column)
                {
                    case IProperty property:
                        columns.Add($"[{property.GetColumnName()}] {property.GetColumnType()}");
                        break;
                    case INavigation navigation:
                        columns.AddRange(navigation.GetTargetType().GetProperties().Where(property => !property.IsPrimaryKey()).Select(property => $"[{property.GetColumnName()}] {property.GetColumnType()}"));
                        break;
                    default:
                        throw new NotSupportedException("Property or navigation type not supported.");
                }
            }

            var command = $"CREATE TABLE {GetTableName()} ({string.Join(", ", columns)})";

            await _db.Database.ExecuteSqlRawAsync(command, cancellationToken);

            return new MergeSourceTable(this, _builder, _loader);
        }

        internal async ValueTask DropTableAsync()
        {
            await _db.Database.ExecuteSqlRawAsync($"DROP TABLE {GetTableName()}");
        }

        public IEnumerable<IPropertyBase> GetProperties()
        {
            return _properties;
        }

        public string GetTableName()
        {
            return _table;
        }
    }
}