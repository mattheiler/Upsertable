using Marvolo.EntityFramework.SqlMerge.Tests.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marvolo.EntityFramework.SqlMerge.Tests.Contexts
{
    public class FooDbContext : DbContext
    {
        public FooDbContext(DbContextOptions<FooDbContext> options)
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
}