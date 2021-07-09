using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public static class MergeExtensions
    {
        public static Type GetMergeProviderClrType(this IProperty property)
        {
            return (Type) property[MergeAnnotations.ProviderClrType];
        }

        public static ValueConverter GetMergeValueConverter(this IProperty property)
        {
            return (ValueConverter) property[MergeAnnotations.ValueConverter];
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