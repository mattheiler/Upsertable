using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Upsertable.Internal.Extensions;

namespace Upsertable;

public class Source(DbContext db, IEnumerable<IPropertyBase> properties, IDataLoader loader, IEnumerable<IDataResolver> resolvers)
{
    private readonly IList<IPropertyBase> _properties = properties.ToList();
    private readonly string _table = "#SOURCE_" + Guid.NewGuid().ToString().Replace('-', '_');

    public async Task<SourceTable> CreateTableAsync(CancellationToken cancellationToken = default)
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

        await db.Database.ExecuteSqlRawAsync(command, cancellationToken);

        return new SourceTable(this, loader, resolvers.ToDictionary(resolver => resolver.Type));
    }

    public async Task DropTableAsync()
    {
#pragma warning disable EF1002
        await db.Database.ExecuteSqlRawAsync($"DROP TABLE {GetTableName()}");
#pragma warning restore EF1002
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