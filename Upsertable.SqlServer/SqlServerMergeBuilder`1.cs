using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Upsertable.Data;

namespace Upsertable.SqlServer;

public class SqlServerMergeBuilder<T> : SqlServerMergeBuilder where T : class
{
    public SqlServerMergeBuilder(DbContext db, IEntityType entityType, EntityProviderFunc provider)
        : base(db, entityType, provider)
    {
    }

    public SqlServerMergeBuilder<T> Merge<TProperty>(Expression<Func<T, TProperty>> property, Action<SqlServerMergeBuilder<TProperty>> build) where TProperty : class
    {
        return (SqlServerMergeBuilder<T>)base.Merge(property, build);
    }

    public SqlServerMergeBuilder<T> MergeMany<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> property, Action<SqlServerMergeBuilder<TProperty>> build) where TProperty : class
    {
        return (SqlServerMergeBuilder<T>)Merge(property, build);
    }

    public new SqlServerMergeBuilder<T> WithSourceLoader(IDataLoader loader)
    {
        return (SqlServerMergeBuilder<T>)base.WithSourceLoader(loader);
    }

    public new SqlServerMergeBuilder<T> On(IReadOnlyCollection<IProperty> on)
    {
        return (SqlServerMergeBuilder<T>)base.On(on);
    }

    public new SqlServerMergeBuilder<T> WithBehavior(MergeBehavior behavior, bool enable = true)
    {
        return (SqlServerMergeBuilder<T>)base.WithBehavior(behavior, enable);
    }

    public new SqlServerMergeBuilder<T> Insert(IReadOnlyCollection<IPropertyBase> insert)
    {
        return (SqlServerMergeBuilder<T>)base.Insert(insert);
    }

    public new SqlServerMergeBuilder<T> Update(IReadOnlyCollection<IPropertyBase> update)
    {
        return (SqlServerMergeBuilder<T>)base.Update(update);
    }
}