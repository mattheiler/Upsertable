using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace Upsertable.SqlServer.Tests;

public class TestDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TestDbContext>
{
    public TestDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();

        services.AddOptions<TestDbContext>();
        services.AddDbContext<TestDbContext>(db =>
        {
            db.UseSqlServer(@"Server=.,61805;Database=Test;User Id=SA;Password=YourStrong@Passw0rd;TrustServerCertificate=True;Encrypt=False", sql =>
                sql.UseNetTopologySuite());
        });

        var provider = services.BuildServiceProvider();
        var context = provider.GetService<TestDbContext>();

        return context;
    }
}