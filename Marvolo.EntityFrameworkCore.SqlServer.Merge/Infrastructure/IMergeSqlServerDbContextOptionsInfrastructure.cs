using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Infrastructure
{
    public interface IMergeSqlServerDbContextOptionsInfrastructure
    {
        SqlServerDbContextOptionsBuilder OptionsBuilder { get; }
    }
}