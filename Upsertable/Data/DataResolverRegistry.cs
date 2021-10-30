using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Upsertable.Data
{
    public class DataResolverRegistry
    {
        private readonly IDictionary<Type, ServiceDescriptor> _resolvers = new Dictionary<Type, ServiceDescriptor>();

        public void Add<T, TDataResolver>(Func<IServiceProvider, TDataResolver> factory, ServiceLifetime lifetime = ServiceLifetime.Transient) where TDataResolver : DataResolver<T>
        {
            _resolvers.Add(typeof(T), new ServiceDescriptor(typeof(DataResolver<>).MakeGenericType(typeof(T)), factory, lifetime));
        }

        public DataResolverProvider GetProvider()
        {
            var services = new ServiceCollection();

            foreach (var service in _resolvers.Values) services.Add(service);

            var provider = services.BuildServiceProvider();

            return new DataResolverProvider(provider);
        }
    }
}