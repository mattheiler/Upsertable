using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Upsertable.EntityFramework.Extensions;

namespace Upsertable.EntityFramework.Data
{
    public static class EntityProvider
    {
        public static EntityProviderFunc Join(DbContext dbContext, ISkipNavigation skipNavigation, EntityProviderFunc declaringEntityProviderFunc)
        {
            return () =>
            {
                var entities = new List<object>();

#pragma warning disable EF1001 // Internal EF Core API usage.
                var instanceFactory = skipNavigation.JoinEntityType.GetInstanceFactory();
#pragma warning restore EF1001 // Internal EF Core API usage.
                var materializationContext = new MaterializationContext(new ValueBuffer(), dbContext);

                foreach (var source in declaringEntityProviderFunc())
                foreach (var target in (IEnumerable)skipNavigation.GetCollectionAccessor().GetOrCreate(source, false))
                {
                    var instance = instanceFactory(materializationContext);

                    skipNavigation.ForeignKey.Properties.SetValues(instance, skipNavigation.ForeignKey.PrincipalKey.Properties.GetValues(source));
                    skipNavigation.Inverse.ForeignKey.Properties.SetValues(instance, skipNavigation.Inverse.ForeignKey.PrincipalKey.Properties.GetValues(target));

                    entities.Add(instance);
                }

                return entities;
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

                return entities;
            };
        }

        public static EntityProviderFunc List<T>(IEnumerable<T> entities)
        {
            return entities.ToList;
        }
    }
}