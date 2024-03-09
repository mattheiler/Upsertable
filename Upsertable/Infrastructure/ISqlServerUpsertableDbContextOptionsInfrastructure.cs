using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Upsertable.SqlServer.Infrastructure;

public interface ISqlServerUpsertableDbContextOptionsInfrastructure
{
    SqlServerDbContextOptionsBuilder OptionsBuilder { get; }
}