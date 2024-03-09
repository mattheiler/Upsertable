using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Upsertable.Extensions;

public static class EntityTypeExtensions
{
    public static INavigationBase? FindNavigationBase(this IEntityType @this, string name)
    {
        return @this.FindNavigation(name) ?? @this.FindSkipNavigation(name) as INavigationBase;
    }

    public static INavigationBase? FindNavigationBase(this IEntityType @this, MemberInfo info)
    {
        return @this.FindNavigation(info) ?? @this.FindSkipNavigation(info) as INavigationBase;
    }

    public static IEnumerable<IPropertyBase> GetPropertiesAndNavigations<T, TProperty>(this IEntityType @this, Expression<Func<T, TProperty>> lambda)
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

                yield return property;

                break;
            }
            case ParameterExpression:
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