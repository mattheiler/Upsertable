using System.Collections;

namespace Marvolo.EntityFramework.SqlMerge
{
    public interface IMergeResolver
    {
        IEnumerable Resolve(MergeContext context);
    }
}