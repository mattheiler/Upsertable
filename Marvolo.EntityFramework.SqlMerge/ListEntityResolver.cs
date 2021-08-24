using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Marvolo.EntityFramework.SqlMerge
{
    public class ListEntityResolver<T> : IEntityResolver
    {
        private readonly List<T> _entities;

        public ListEntityResolver(IEnumerable<T> entities)
        {
            _entities = entities.ToList();
        }

        public IEnumerable Resolve()
        {
            return _entities;
        }
    }
}