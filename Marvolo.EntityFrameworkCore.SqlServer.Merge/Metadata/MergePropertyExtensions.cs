using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Metadata
{
    public static class MergePropertyExtensions
    {
        public static Type GetMergeProviderClrType(this IProperty property)
        {
            return (Type) property[MergePropertyAnnotations.ProviderClrType];
        }

        public static ValueConverter GetMergeValueConverter(this IProperty property)
        {
            return (ValueConverter) property[MergePropertyAnnotations.ValueConverter];
        }
    }
}