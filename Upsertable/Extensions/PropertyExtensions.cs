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
        var name = property.DeclaringType.GetTableName();
        if (name == null) throw new InvalidOperationException("Declaring type name is null.");
        var schema = property.DeclaringType.GetSchema();
        var table = StoreObjectIdentifier.Table(name, schema);
        return property.GetColumnName(table);
    }

    public static object GetValue(this IPropertyBase property, object obj)
    {
        return property.GetGetter().GetClrValue(obj);
    }

    public static object[] GetValues(this IEnumerable<IPropertyBase> properties, object obj)
    {
        return properties.Select(property => property.GetValue(obj)).ToArray();
    }

    public static void SetValue(this IPropertyBase property, object obj, object value)
    {
        var info = property.PropertyInfo;
        if (info == null) throw new InvalidOperationException("Property info is null.");
        if (property.IsIndexerProperty())
            info.SetValue(obj, value, new object[] { property.Name });
        else
            info.SetValue(obj, value);
    }

    public static void SetValues(this IReadOnlyList<IPropertyBase> properties, object obj, IReadOnlyList<object> values, int offset = 0)
    {
        for (var index = 0; index < properties.Count; index++) properties[index].SetValue(obj, values[offset + index]);
    }
}