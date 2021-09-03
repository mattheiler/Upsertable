using System.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Marvolo.EntityFramework.SqlMerge
{
    public class JoinEntityResolver : IEntityResolver
    {
        private readonly DbContext _dbContext;
        private readonly ISkipNavigation _skipNavigation;
        private readonly IEntityResolver _declaringEntityResolver;

        public JoinEntityResolver(DbContext dbContext, ISkipNavigation skipNavigation, IEntityResolver declaringEntityResolver)
        {
            _dbContext = dbContext;
            _skipNavigation = skipNavigation;
            _declaringEntityResolver = declaringEntityResolver;
        }

        public IEnumerable Resolve()
        {
            var instanceFactory = _skipNavigation.JoinEntityType.GetInstanceFactory();
            var materializationContext = new MaterializationContext(new ValueBuffer(), _dbContext);

            foreach (var source in _declaringEntityResolver.Resolve())
            foreach (var target in (IEnumerable) _skipNavigation.GetCollectionAccessor().GetOrCreate(source, false))
            {
                var instance = instanceFactory(materializationContext);

                _skipNavigation.ForeignKey.Properties.SetValues(instance, _skipNavigation.ForeignKey.PrincipalKey.Properties.GetValues(source));
                _skipNavigation.Inverse.ForeignKey.Properties.SetValues(instance, _skipNavigation.Inverse.ForeignKey.PrincipalKey.Properties.GetValues(target));

                yield return instance;
            }
        }
    }
}