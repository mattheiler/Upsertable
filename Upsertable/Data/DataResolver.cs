using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Upsertable.Abstractions;

namespace Upsertable.Data
{
    public abstract class DataResolver<T> : IDataResolver
    {
        public abstract object ResolveData(IProperty property, object value);

        public abstract Type ResolveDataType(IProperty property);

        public virtual object ResolveData(IProperty property, T value)
        {
            return ResolveData(property, (object)value);
        }
    }
}