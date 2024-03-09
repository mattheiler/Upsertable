using Microsoft.EntityFrameworkCore;
using Upsertable.SqlServer.Tests.Entities;

namespace Upsertable.SqlServer.Tests;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    public DbSet<Foo> Foos { get; set; }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Fub>().HasKey(fub => new { fub.FooId, fub.BazId });

        model.Entity<Ack>().OwnsOne(ack => ack.Bar).WithOwner();

        model.Entity<Qux>().HasKey(qux => qux.BazId);
        model.Entity<Qux>().OwnsMany(qux => qux.Fums).WithOwner().HasForeignKey(nameof(Fum.OwnerId));
    }
}