using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Upsertable.Infrastructure;

public interface ISqlServerUpsertableDbContextOptionsInfrastructure
{
    SqlServerDbContextOptionsBuilder OptionsBuilder { get; }
}