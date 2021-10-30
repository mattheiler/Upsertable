using System;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Upsertable.Abstractions
{
    public interface IDataResolver
    {
        object ResolveData(IProperty property, object value);

        Type ResolveDataType(IProperty property);
    }
}