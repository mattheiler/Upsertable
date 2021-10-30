using System;
using Upsertable.Abstractions;

namespace Upsertable.Data
{
    public class DataResolverProvider
    {
        private readonly IServiceProvider _provider;

        public DataResolverProvider(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IDataResolver GetResolver(Type type)
        {
            return (IDataResolver)_provider.GetService(typeof(DataResolver<>).MakeGenericType(type));
        }
    }
}