using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Internal
{
    internal static class MergeEntityTypeExtensions
    {
        public static IEnumerable<IPropertyBase> GetPropertiesAndOwnedNavigations(this IEntityType @this)
        {
            return @this.GetProperties().Concat<IPropertyBase>(@this.GetOwnedNavigations());
        }

        public static IEnumerable<IPropertyBase> GetPropertiesAndNavigations<T, TProperty>(this IEntityType @this, Expression<Func<T, TProperty>> lambda)
        {
            return lambda.Body switch
            {
                NewExpression @new => @this.GetPropertiesAndNavigations(@new),
                MemberExpression member => @this.GetPropertiesAndNavigations(member),
                _ => throw new NotSupportedException($"Expression not supported: '{lambda}'.")
            };
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

        private static IEnumerable<IPropertyBase> GetPropertiesAndNavigations(this IEntityType type, NewExpression @new)
        {
            return
                from argument in @new.Arguments
                let member = (MemberExpression)argument
                from property in type.GetPropertiesAndNavigations(member)
                select property;
        }

        private static IEnumerable<IPropertyBase> GetPropertiesAndNavigations(this IEntityType type, MemberExpression member)
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