using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public class MergeContext
    {
        private readonly ConcurrentDictionary<string, IEnumerable> _entities = new ConcurrentDictionary<string, IEnumerable>();

        public MergeContext(DbContext db)
        {
            Db = db;
        }

        public DbContext Db { get; }

        public void Add<T>(T entity)
        {
            AddRange(typeof(T), new[] { entity });
        }

        public void Add(Type type, object entity)
        {
            AddRange(type, new[] { entity });
        }

        public void AddRange<T>(IEnumerable<T> entities)
        {
            AddRange(typeof(T), entities);
        }

        public void AddRange(Type type, IEnumerable entities)
        {
            _entities.AddOrUpdate(type.Name, _ => new HashSet<object>(entities.Cast<object>()), (_, list) =>
            {
                var set = (HashSet<object>) list;
                foreach (var entity in entities) set.Add(entity);
                return list;
            });
        }

        public IEnumerable Get(Type type)
        {
            return _entities.TryGetValue(type.Name, out var entities) ? entities : Enumerable.Empty<object>();
        }

        public IEnumerable<T> Get<T>()
        {
            return Get(typeof(T)).Cast<T>();
        }
    }
}