using System.Runtime.Serialization;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public enum MergeAction
    {
        [EnumMember(Value = "INSERT")]
        Insert,

        [EnumMember(Value = "UPDATE")]
        Update,

        [EnumMember(Value = "DELETE")]
        Delete
    }
}