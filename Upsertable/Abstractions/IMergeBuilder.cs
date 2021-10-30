namespace Upsertable.EntityFramework.Abstractions
{
    public interface IMergeBuilder
    {
        IMerge ToMerge();
    }
}