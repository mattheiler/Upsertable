using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Upsertable.Data;

namespace Upsertable.SqlServer;

public static class MergeBuilderExtensions
{
    public static MergeBuilder<T> Merge<T>(this DbContext @this, IEnumerable<T> entities) where T : class
    {
        return new MergeBuilder<T>(@this, @this.Model.FindEntityType(typeof(T)), EntityProvider.List(entities));
    }

    public static MergeBuilder<T> Insert<T>(this MergeBuilder<T> @this) where T : class
    {
        return @this.WithBehavior(MergeBehavior.Insert);
    }

    public static MergeBuilder<T> Insert<T, TProperty>(this MergeBuilder<T> @this, Expression<Func<T, TProperty>> insert) where T : class
    {
        return @this.WithBehavior(MergeBehavior.Insert).Insert(@this.EntityType.GetPropertiesAndNavigations(insert).ToList());
    }

    public static MergeBuilder<T> On<T, TProperty>(this MergeBuilder<T> @this, Expression<Func<T, TProperty>> on) where T : class
    {
        return @this.On(@this.EntityType.GetPropertiesAndNavigations(on).Cast<IProperty>().ToList());
    }

    public static MergeBuilder<T> Update<T>(this MergeBuilder<T> @this) where T : class
    {
        return @this.WithBehavior(MergeBehavior.Update);
    }

    public static MergeBuilder<T> Update<T, TProperty>(this MergeBuilder<T> @this, Expression<Func<T, TProperty>> update) where T : class
    {
        return @this.WithBehavior(MergeBehavior.Update).Update(@this.EntityType.GetPropertiesAndNavigations(update).ToList());
    }

    private static IEnumerable<IPropertyBase> GetPropertiesAndNavigations<T, TProperty>(this IEntityType @this, Expression<Func<T, TProperty>> lambda)
    {
        return lambda.Body switch
        {
            NewExpression @new => @this.GetPropertiesAndNavigations(@new),
            MemberExpression member => @this.GetPropertiesAndNavigations(member),
            _ => throw new NotSupportedException($"Unexpected expression: '{lambda}'.")
        };
    }

    private static IEnumerable<IPropertyBase> GetPropertiesAndNavigations(this IEntityType type, NewExpression @new)
    {
        return
            from argument in @new.Arguments
            let member = argument as MemberExpression ?? throw new InvalidOperationException($"Expected a property: '{argument}'.")
            from property in type.GetPropertiesAndNavigations(member)
            select property;
    }

    private static IEnumerable<IPropertyBase> GetPropertiesAndNavigations(this IEntityType type, MemberExpression member)
    {
        switch (member.Expression)
        {
            case MemberExpression caller:
            {
                var navigation = type.FindNavigation(caller.Member) ?? throw new InvalidOperationException($"Expected a navigation property: '{caller}'.");
                var entity = navigation.TargetEntityType;
                if (!entity.IsOwned())
                    throw new InvalidOperationException($"Expected an owned navigation property: '{caller}'.");

                var property = entity.FindProperty(member.Member) ?? throw new InvalidOperationException($"Expected a property: '{member}'.");
                if (property == null)
                    throw new InvalidOperationException($"Expected a property: '{member}'.");

                yield return property;

                break;
            }
            case ParameterExpression _:
            {
                var property = type.FindProperty(member.Member);
                if (property != null)
                {
                    yield return property;
                }
                else
                {
                    var navigation = type.FindNavigation(member.Member) ?? throw new InvalidOperationException($"Expected a navigation property: '{member}'.");
                    var entity = type.Model.FindEntityType(navigation.ClrType) ?? throw new InvalidOperationException("Entity type not found.");
                    if (!entity.IsOwned())
                        throw new InvalidOperationException($"Expected an owned navigation property: '{member}'.");

                    foreach (var owned in entity.GetProperties())
                        yield return owned;
                }

                break;
            }
            default:
                throw new NotSupportedException($"Unexpected expression: '{member.Expression}'.");
        }
    }
}