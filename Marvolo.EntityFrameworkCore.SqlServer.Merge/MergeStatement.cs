namespace Marvolo.EntityFrameworkCore.SqlServer.Merge
{
    public readonly struct MergeStatement
    {
        private readonly string _sql;

        public MergeStatement(string sql)
        {
            _sql = sql;
        }

        public override int GetHashCode()
        {
            return _sql.GetHashCode();
        }

        public override string ToString()
        {
            return _sql;
        }
    }
}