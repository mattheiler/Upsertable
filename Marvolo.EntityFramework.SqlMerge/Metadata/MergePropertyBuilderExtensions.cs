using System;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Marvolo.EntityFramework.SqlMerge.Metadata
{
    public static class MergePropertyBuilderExtensions
    {
        public static PropertyBuilder<TProperty> HasMergeProviderClrType<TProperty>(this PropertyBuilder<TProperty> property, Type type)
        {
            return property.HasAnnotation(MergePropertyAnnotations.ProviderClrType, type);
        }

        public static PropertyBuilder<TProperty> HasMergeValueConverter<TProperty, TProvider>(this PropertyBuilder<TProperty> property, ValueConverter<TProperty, TProvider> converter)
        {
            return property.HasAnnotation(MergePropertyAnnotations.ValueConverter, converter);
        }
    }
}