using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Marvolo.EntityFramework.SqlMerge.Infrastructure
{
    public interface IMergeSqlServerDbContextOptionsInfrastructure
    {
        SqlServerDbContextOptionsBuilder OptionsBuilder { get; }
    }
}