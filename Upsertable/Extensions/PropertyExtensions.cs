using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Upsertable.Extensions;

public static class PropertyExtensions
{
    public static string GetColumnNameInTable(this IProperty property)
    {
        var type = property.DeclaringType;
        var name = type.GetTableName() ?? throw new InvalidOperationException("Declaring type must be mapped to a table.");
        var schema = type.GetSchema();
        var table = StoreObjectIdentifier.Table(name, schema);
        var column = property.GetColumnName(table) ?? throw new InvalidOperationException("Property must be mapped to a column.");
        return column;
    }

    public static object? GetValue(this IPropertyBase property, object obj)
    {
        return property.GetGetter().GetClrValue(obj);
    }

    public static object?[] GetValues(this IEnumerable<IPropertyBase> properties, object obj)
    {
        return properties.Select(property => property.GetValue(obj)).ToArray();
    }

    public static void SetValue(this IPropertyBase property, object obj, object? value)
    {
        var info = property.PropertyInfo ?? throw new InvalidOperationException("Property must not be a shadow property or mapped directly to a field.");
        if (property.IsIndexerProperty())
            info.SetValue(obj, value, [property.Name]);
        else
            info.SetValue(obj, value);
    }

    public static void SetValues(this IReadOnlyList<IPropertyBase> properties, object obj, IReadOnlyList<object?> values, int offset = 0)
    {
        for (var index = 0; index < properties.Count; index++) properties[index].SetValue(obj, values[offset + index]);
    }
}