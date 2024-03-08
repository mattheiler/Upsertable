using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Upsertable.Abstractions;
using Upsertable.Extensions;

namespace Upsertable.SqlServer;

public class SqlServerMergeSource
{
    private readonly DbContext _db;
    private readonly IList<IPropertyBase> _properties;
    private readonly string _table = "#SOURCE_" + Guid.NewGuid().ToString().Replace('-', '_');
    private readonly IDataTableLoader _tableLoader;
    private readonly IDataTableFactory _tableResolver;

    public SqlServerMergeSource(DbContext db, IEnumerable<IPropertyBase> properties, IDataTableFactory tableResolver, IDataTableLoader tableLoader)
    {
        _db = db;
        _properties = properties.ToList();
        _tableResolver = tableResolver;
        _tableLoader = tableLoader;
    }

    public async Task<SqlServerMergeSourceTable> CreateTableAsync(CancellationToken cancellationToken = default)
    {
        var columns = new List<string>();

        foreach (var column in GetProperties())
            switch (column)
            {
                case IProperty property:
                    columns.Add($"[{property.GetColumnNameInTable()}] {property.GetColumnType()}");
                    break;
                case INavigation navigation:
                    columns.AddRange(navigation.TargetEntityType.GetProperties().Where(property => !property.IsPrimaryKey()).Select(property => $"[{property.GetColumnNameInTable()}] {property.GetColumnType()}"));
                    break;
                default:
                    throw new NotSupportedException("Property or navigation type not supported.");
            }

        var command = $"CREATE TABLE {GetTableName()} ({string.Join(", ", columns)})";

        await _db.Database.ExecuteSqlRawAsync(command, cancellationToken);

        return new SqlServerMergeSourceTable(this, _tableResolver, _tableLoader);
    }

    public async Task DropTableAsync()
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