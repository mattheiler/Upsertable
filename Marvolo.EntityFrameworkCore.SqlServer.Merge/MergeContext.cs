using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeContext
    {
        private readonly ConcurrentDictionary<string, IEnumerable> _entities = new ConcurrentDictionary<string, IEnumerable>();

        public void Add<T>(T entity)
        {
            AddRange(typeof(T), entity);
        }

        public void Add(Type type, object entity)
        {
            AddRange(type, entity);
        }

        public void AddRange<T>(params T[] entities)
        {
            AddRange(entities.OfType<T>());
        }

        public void AddRange<T>(IEnumerable<T> entities)
        {
            AddRange(typeof(T), entities);
        }

        public void AddRange(Type type, params object[] entities)
        {
            AddRange(type, entities.AsEnumerable());
        }

        public void AddRange(Type type, IEnumerable entities)
        {
            AddRange(type, entities.Cast<object>());
        }

        public void AddRange(Type type, IEnumerable<object> entities)
        {
            _entities.AddOrUpdate(type.FullName!, _ => new HashSet<object>(entities), (_, list) =>
            {
                foreach (var entity in entities) ((HashSet<object>) list).Add(entity);
                return list;
            });
        }

        public bool Contains(Type type)
        {
            return _entities.ContainsKey(type.FullName!);
        }

        public IEnumerable Get(Type type)
        {
            return _entities.TryGetValue(type.FullName!, out var entities) ? entities : Enumerable.Empty<object>();
        }

        public IEnumerable<T> Get<T>()
        {
            return Get(typeof(T)).Cast<T>();
        }
    }
}