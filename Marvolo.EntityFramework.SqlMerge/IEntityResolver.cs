using System.Collections;

namespace Marvolo.EntityFramework.SqlMerge
{
    public interface IEntityResolver
    {
        IEnumerable Resolve();
    }
}