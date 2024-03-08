using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Upsertable.Abstractions;

namespace Upsertable.Data;

public class DataResolver<T> : IDataResolver
{
    private readonly Func<IProperty, T, object> _resolveData;
    private readonly Func<IProperty, Type> _resolveDataType;

    public DataResolver(Func<IProperty, Type> resolveDataType, Func<IProperty, T, object> resolveData)
    {
        _resolveDataType = resolveDataType;
        _resolveData = resolveData;
    }

    public Type Type => typeof(T);

    public object ResolveData(IProperty property, object value)
    {
        return _resolveData(property, (T)value);
    }

    public Type ResolveDataType(IProperty property)
    {
        return _resolveDataType(property);
    }
}