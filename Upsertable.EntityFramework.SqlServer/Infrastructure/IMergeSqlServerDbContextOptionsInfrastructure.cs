using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Upsertable.EntityFramework.SqlServer.Infrastructure
{
    public interface IMergeSqlServerDbContextOptionsInfrastructure
    {
        SqlServerDbContextOptionsBuilder OptionsBuilder { get; }
    }
}