using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Marvolo.EntityFramework.SqlMerge
{
    public class LazyEntityResolver : IEntityResolver
    {
        private readonly INavigationBase _navigation;
        private readonly IEntityResolver _declaringEntityResolver;

        public LazyEntityResolver(INavigationBase navigationBase, IEntityResolver declaringEntityResolver)
        {
            _navigation = navigationBase;
            _declaringEntityResolver = declaringEntityResolver;
        }

        public IEnumerable Resolve()
        {
            return GetEntities().Distinct();
        }

        private IEnumerable<object> GetEntities()
        {
            foreach (var source in _declaringEntityResolver.Resolve())
            {
                var value = _navigation.GetValue(source);
                if (value == null)
                    continue;

                if (_navigation.IsCollection)
                    foreach (var item in (ICollection) value)
                        yield return item;
                else
                    yield return value;
            }
        }
    }
}