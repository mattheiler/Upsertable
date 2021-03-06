using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upsertable.SqlServer;
using Upsertable.SqlServer.Infrastructure.Extensions;
using Upsertable.SqlServer.SqlBulkCopy.Infrastructure.Extensions;
using Upsertable.Tests.Contexts;
using Upsertable.Tests.Entities;
using Xunit;

namespace Upsertable.Tests
{
    public class MasterTest
    {
        private readonly FooDbContext _context;

        public MasterTest()
        {
            var services = new ServiceCollection();

            services.AddDbContext<FooDbContext>(db => db.UseSqlServer("Server=.;Database=Foo;Trusted_Connection=True", sql => sql.UseUpsertable(merge => merge.UseSqlBulkCopy())));

            var provider = services.BuildServiceProvider();
            var context = provider.GetService<FooDbContext>();

            _context = context;
        }

        [Fact]
        public void Test1()
        {
            var statement =
                _context
                    .Merge(Enumerable.Empty<Foo>())
                    .On(foo => foo.Code)
                    .Insert()
                    .Update(foo => foo.Name)
                    .MergeMany(foo => foo.Fubs, fubs =>
                    {
                        fubs
                            .Insert()
                            .Merge(fub => fub.Baz, bazs =>
                            {
                                bazs.On(baz => baz.Code)
                                    .Insert()
                                    .Merge(baz => baz.Qux, quxs =>
                                    {
                                        quxs.On(fum => fum.Code)
                                            .Insert()
                                            .MergeMany(qux => qux.Fums, fums =>
                                            {
                                                fums.On(fum => fum.Code)
                                                    .Insert();
                                            });
                                    });
                            });
                    })
                    .MergeMany(foo => foo.Acks, acks =>
                    {
                        acks.On(bar => bar.Code)
                            .Insert()
                            .Update(ack => ack.Name)
                            .Merge(ack => ack.Bar, bars =>
                            {
                                bars.On(bar => bar.Code)
                                    .Insert();
                            });
                    })
                    .ToMerge()
                    .ToString();

            Assert.True(true);
        }
    }
}