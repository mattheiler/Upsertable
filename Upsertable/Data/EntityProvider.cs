using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Upsertable.Extensions;

namespace Upsertable.Data
{
    public static class EntityProvider
    {
        public static EntityProviderFunc Join(DbContext dbContext, ISkipNavigation skipNavigation, EntityProviderFunc declaringEntityProviderFunc)
        {
            return () =>
            {
                var entities = new List<object>();

#pragma warning disable EF1001 // Internal EF Core API usage.
                var materializationContext = new MaterializationContext(new ValueBuffer(), dbContext);
                var materializer = dbContext.GetDependencies().StateManager.EntityMaterializerSource.GetMaterializer(skipNavigation.JoinEntityType);
#pragma warning restore EF1001 // Internal EF Core API usage.

                foreach (var source in declaringEntityProviderFunc())
                foreach (var target in (IEnumerable) skipNavigation.GetCollectionAccessor()?.GetOrCreate(source, false) ?? Enumerable.Empty<object>())
                {
                    var instance = materializer(materializationContext);

                    skipNavigation.ForeignKey.Properties.SetValues(instance, skipNavigation.ForeignKey.PrincipalKey.Properties.GetValues(source));
                    skipNavigation.Inverse.ForeignKey.Properties.SetValues(instance, skipNavigation.Inverse.ForeignKey.PrincipalKey.Properties.GetValues(target));

                    entities.Add(instance);
                }

                return entities.Distinct();
            };
        }

        public static EntityProviderFunc Lazy(INavigationBase navigationBase, EntityProviderFunc declaringEntityProviderFunc)
        {
            return () =>
            {
                var entities = new List<object>();

                foreach (var source in declaringEntityProviderFunc())
                {
                    var value = navigationBase.GetValue(source);
                    if (value == null)
                        continue;

                    if (navigationBase.IsCollection)
                        entities.AddRange(((ICollection)value).Cast<object>());
                    else
                        entities.Add(value);
                }

                return entities.Distinct();
            };
        }

        public static EntityProviderFunc List<T>(IEnumerable<T> entities)
        {
            return entities.Distinct;
        }
    }
}