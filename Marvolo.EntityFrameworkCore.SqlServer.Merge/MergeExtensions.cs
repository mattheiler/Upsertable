using System;
using System.Collections.Generic;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Abstractions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public static class MergeExtensions
    {
        internal static IMerge ToComposite(this IEnumerable<IMerge> @this)
        {
            return new MergeComposite(@this);
        }

        internal static IMerge WithNoTracking(this IMerge @this)
        {
            return new MergeWithNoTracking(@this);
        }

        public static Type GetMergeProviderClrType(this IProperty property)
        {
            return (Type)property[MergeAnnotations.ProviderClrType];
        }

        public static ValueConverter GetMergeValueConverter(this IProperty property)
        {
            return (ValueConverter)property[MergeAnnotations.ValueConverter];
        }

        public static PropertyBuilder<TProperty> HasMergeProviderClrType<TProperty>(this PropertyBuilder<TProperty> property, Type type)
        {
            return property.HasAnnotation(MergeAnnotations.ProviderClrType, type);
        }

        public static PropertyBuilder<TProperty> HasMergeValueConverter<TProperty, TProvider>(this PropertyBuilder<TProperty> property, ValueConverter<TProperty, TProvider> converter)
        {
            return property.HasAnnotation(MergeAnnotations.ValueConverter, converter);
        }
    }
}