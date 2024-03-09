using System;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Upsertable;

public interface IDataResolver
{
    Type Type { get; }

    object? ResolveData(IProperty property, object? value);

    Type ResolveDataType(IProperty property);
}