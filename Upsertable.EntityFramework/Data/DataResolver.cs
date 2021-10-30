using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Upsertable.EntityFramework.Abstractions;

namespace Upsertable.EntityFramework.Data
{
    public abstract class DataResolver<T> : IDataResolver
    {
        public virtual object ResolveData(IProperty property, T value)
        {
            return ResolveData(property, (object)value);
        }

        public abstract object ResolveData(IProperty property, object value);

        public abstract Type ResolveDataType(IProperty property);
    }
}