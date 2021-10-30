using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Upsertable.SqlServer.Infrastructure
{
    public interface IMergeSqlServerDbContextOptionsInfrastructure
    {
        SqlServerDbContextOptionsBuilder OptionsBuilder { get; }
    }
}