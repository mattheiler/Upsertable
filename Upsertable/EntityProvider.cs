using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Upsertable.Internal.Extensions;

namespace Upsertable;

public class EntityProvider
{
    public static EntityProviderFunc Join(DbContext db, ISkipNavigation navigation, EntityProviderFunc provider)
    {
        return () =>
        {
            var entities = new List<object>();

#pragma warning disable EF1001 // Internal EF Core API usage.
            var context = new MaterializationContext(default, db);
            var materializer = db.GetDependencies().StateManager.EntityMaterializerSource.GetMaterializer(navigation.JoinEntityType);
#pragma warning restore EF1001 // Internal EF Core API usage.

            var accessor = navigation.GetCollectionAccessor() ?? throw new InvalidOperationException("Navigation must be a collection.");

            foreach (var data in provider())
            {
                var items = (IEnumerable)accessor.GetOrCreate(data, false);

                foreach (var item in items)
                {
                    var instance = materializer(context);

                    navigation.ForeignKey.Properties.SetValues(instance, navigation.ForeignKey.PrincipalKey.Properties.GetValues(data));
                    navigation.Inverse.ForeignKey.Properties.SetValues(instance, navigation.Inverse.ForeignKey.PrincipalKey.Properties.GetValues(item));

                    entities.Add(instance);
                }
            }

            return entities.Distinct();
        };
    }

    public static EntityProviderFunc Lazy(INavigationBase navigation, EntityProviderFunc provider)
    {
        return () =>
        {
            var entities = new List<object>();

            foreach (var source in provider())
            {
                var value = navigation.GetValue(source);
                if (value == null)
                    continue;

                if (navigation.IsCollection)
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