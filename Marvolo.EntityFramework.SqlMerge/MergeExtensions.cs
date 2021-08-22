using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFramework.SqlMerge
{
    internal static class MergeExtensions
    {
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
            if (property.IsIndexerProperty())
                property.PropertyInfo.SetValue(obj, value, new object[] { property.Name });
            else
                property.PropertyInfo.SetValue(obj, value);
        }

        public static void SetValues(this IReadOnlyList<IPropertyBase> properties, object obj, IReadOnlyList<object> values, int offset = 0)
        {
            for (var index = 0; index < properties.Count; index++) properties[index].SetValue(obj, values[offset + index]);
        }
    }
}