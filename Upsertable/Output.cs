using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Upsertable.Internal.Extensions;

namespace Upsertable;

public class Output(DbContext db, IEnumerable<IProperty> properties)
{
    private readonly IList<IProperty> _properties = properties.ToList();
    private readonly string _table = "#OUTPUT_" + Guid.NewGuid().ToString().Replace('-', '_');

    public async Task<OutputTable> CreateTableAsync(CancellationToken cancellationToken = default)
    {
        var columns = GetProperties().Select(property => $"{property.GetColumnNameInTable()} {property.GetColumnType()}");
        var command = $"CREATE TABLE [{GetTableName()}] ({string.Join(", ", columns)})";

        await db.Database.ExecuteSqlRawAsync(command, cancellationToken);

        return new OutputTable(this);
    }

    public async Task DropTableAsync()
    {
#pragma warning disable EF1002
        await db.Database.ExecuteSqlRawAsync($"DROP TABLE {GetTableName()}");
#pragma warning restore EF1002
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