using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    internal static class MergeExtensionsInternal
    {
        public static IEnumerable<IPropertyBase> GetColumns(this IEntityType @this)
        {
            return @this.GetProperties().Concat<IPropertyBase>(@this.GetOwnedNavigations());
        }

        public static IEnumerable<IPropertyBase> GetColumns(this IEntityType @this, LambdaExpression lambda)
        {
            return lambda.Body switch
            {
                NewExpression @new => GetColumns(@this, @new),
                MemberExpression member => GetColumns(@this, member),
                _ => throw new NotSupportedException($"Expression not supported: '{lambda}'.")
            };
        }

        public static IEnumerable<IProperty> GetColumns(this INavigation navigation)
        {
            return navigation.DeclaringType.Model.FindEntityType(navigation.ClrType).GetProperties().Where(property => !property.IsPrimaryKey());
        }

        private static IEnumerable<INavigation> GetOwnedNavigations(this IEntityType @this)
        {
            return
                from navigation in @this.GetNavigations()
                where !navigation.IsDependentToPrincipal()
                where !navigation.IsCollection()
                let type = @this.Model.FindEntityType(navigation.ClrType)
                where type.IsOwned()
                select navigation;
        }

        private static IEnumerable<IPropertyBase> GetColumns(IEntityType type, NewExpression @new)
        {
            return
                from argument in @new.Arguments
                let member = (MemberExpression) argument
                from property in GetColumns(type, member)
                select property;
        }

        private static IEnumerable<IPropertyBase> GetColumns(IEntityType type, MemberExpression member)
        {
            switch (member.Expression)
            {
                case MemberExpression caller:
                {
                    var navigation = type.FindNavigation(caller.Member);
                    var entity = type.Model.FindEntityType(navigation.ClrType);
                    if (!entity.IsOwned())
                        throw new InvalidOperationException($"Expected an owned type: '{caller}'.");

                    var property = entity.FindProperty(member.Member);
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
                        var navigation = type.FindNavigation(member.Member);
                        var entity = type.Model.FindEntityType(navigation.ClrType);
                        if (!entity.IsOwned())
                            throw new InvalidOperationException($"Expected an owned type: '{member}'.");

                        foreach (var shadow in entity.GetProperties())
                            yield return shadow;
                    }
                    break;
                }
                default:
                    throw new NotSupportedException();
            }
        }
    }
}