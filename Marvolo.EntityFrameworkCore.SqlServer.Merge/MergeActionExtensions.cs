using System;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public static class MergeActionExtensions
    {
        public static string ToSql(this MergeAction action)
        {
            return action switch
            {
                MergeAction.Insert => "INSERT",
                MergeAction.Update => "UPDATE",
                MergeAction.Delete => "DELETE",
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Merge action enum value is out of range.")
            };
        }
    }
}