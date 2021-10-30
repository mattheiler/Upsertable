using System;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Upsertable.EntityFramework.Abstractions
{
    public interface IDataResolver
    {
        object ResolveData(IProperty property, object value);

        Type ResolveDataType(IProperty property);
    }
}