using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Upsertable.SqlServer;

public class MergeBuilder<T>(DbContext db, Func<IEnumerable> provider) : MergeBuilder(db.Model.FindEntityType(typeof(T)) ?? throw new ArgumentException("Entity type not found."), db, provider) where T : class
{
    public MergeBuilder<T> Merge<TProperty>(Expression<Func<T, TProperty>> property, Action<MergeBuilder<TProperty>> build) where TProperty : class
    {
        return (MergeBuilder<T>)base.Merge(property, build);
    }

    public MergeBuilder<T> MergeMany<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> property, Action<MergeBuilder<TProperty>> build) where TProperty : class
    {
        return (MergeBuilder<T>)Merge(property, build);
    }

    public new MergeBuilder<T> WithSourceLoader(IDataLoader loader)
    {
        return (MergeBuilder<T>)base.WithSourceLoader(loader);
    }

    public new MergeBuilder<T> On(IReadOnlyCollection<IProperty> on)
    {
        return (MergeBuilder<T>)base.On(on);
    }

    public new MergeBuilder<T> WithBehavior(MergeBehavior behavior, bool enable = true)
    {
        return (MergeBuilder<T>)base.WithBehavior(behavior, enable);
    }

    public new MergeBuilder<T> Insert(IReadOnlyCollection<IPropertyBase> insert)
    {
        return (MergeBuilder<T>)base.Insert(insert);
    }

    public new MergeBuilder<T> Update(IReadOnlyCollection<IPropertyBase> update)
    {
        return (MergeBuilder<T>)base.Update(update);
    }
}