using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Upsertable.Infrastructure;

internal interface ISqlServerUpsertableDbContextOptionsInfrastructure
{
    SqlServerDbContextOptionsBuilder OptionsBuilder { get; }
}