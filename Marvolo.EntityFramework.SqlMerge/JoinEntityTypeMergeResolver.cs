using System.Collections;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Marvolo.EntityFramework.SqlMerge
{
    public class JoinEntityTypeMergeResolver : IMergeResolver
    {
        private readonly ISkipNavigation _skipNavigation;

        public JoinEntityTypeMergeResolver(ISkipNavigation skipNavigation)
        {
            _skipNavigation = skipNavigation;
        }

        public IEnumerable Resolve(MergeContext context)
        {
            foreach (var declaring in context.Get(_skipNavigation.DeclaringEntityType.ClrType))
            foreach (var target in (IEnumerable) _skipNavigation.GetCollectionAccessor().GetOrCreate(declaring, false))
            {
                var instanceFactory = _skipNavigation.JoinEntityType.GetInstanceFactory();
                var instance = instanceFactory(new MaterializationContext());

                _skipNavigation.ForeignKey.Properties.SetValues(instance, _skipNavigation.ForeignKey.PrincipalKey.Properties.GetValues(declaring));
                _skipNavigation.Inverse.ForeignKey.Properties.SetValues(instance, _skipNavigation.Inverse.ForeignKey.PrincipalKey.Properties.GetValues(target));

                yield return instance;
            }
        }
    }
}