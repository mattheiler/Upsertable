using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    internal static class MergeExtensions
    {
        public static object GetValue(this IPropertyBase property, object obj)
        {
            return property.GetGetter().GetClrValue(obj);
        }

        public static object[] GetValues(this IForeignKey key, object obj)
        {
            return key.Properties.GetValues(obj);
        }

        public static object[] GetValues(this IKey key, object obj)
        {
            return key.Properties.GetValues(obj);
        }

        public static object[] GetValues(this IEnumerable<IProperty> properties, object obj)
        {
            return properties.Select(property => property.GetValue(obj)).ToArray();
        }

        public static void SetValue(this IPropertyBase property, object obj, object value)
        {
            property.PropertyInfo.SetValue(obj, value);
        }

        public static void SetValues(this IForeignKey key, object obj, object[] values, int offset = 0)
        {
            key.Properties.SetValues(obj, values, offset);
        }

        public static void SetValues(this IKey key, object obj, object[] values, int offset = 0)
        {
            key.Properties.SetValues(obj, values, offset);
        }

        public static void SetValues(this IReadOnlyList<IProperty> properties, object obj, IReadOnlyList<object> values, int offset = 0)
        {
            for (var index = 0; index < properties.Count; index++) properties[index].SetValue(obj, values[offset + index]);
        }
    }
}