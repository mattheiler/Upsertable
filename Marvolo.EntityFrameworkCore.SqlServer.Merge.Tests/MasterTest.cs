using System.Linq;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.SqlBulkCopy;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Tests.Contexts;
using Marvolo.EntityFrameworkCore.SqlServer.Merge.Tests.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Tests
{
    public class MasterTest
    {
        private readonly FooDbContext _context;

        public MasterTest()
        {
            var services = new ServiceCollection();

            services
                .AddSqlBulkCopyMergeStrategy()
                .AddDbContext<FooDbContext>(options => options.UseSqlServer("Server=.;Database=Foo;Trusted_Connection=True"));

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
                    .IncludeMany(foo => foo.Fubs, fubs =>
                    {
                        fubs
                            .Insert()
                            .Include(fub => fub.Baz, bazs =>
                            {
                                bazs
                                    .On(baz => baz.Code)
                                    .Insert()
                                    .Include(baz => baz.Qux, quxs =>
                                    {
                                        quxs
                                            .On(fum => fum.Code)
                                            .Insert()
                                            .IncludeMany(qux => qux.Fums, fums =>
                                            {
                                                fums
                                                    .On(fum => fum.Code)
                                                    .Insert();
                                            });
                                    });
                            });
                    })
                    .IncludeMany(foo => foo.Acks, acks =>
                    {
                        acks
                            .On(bar => bar.Code)
                            .Insert()
                            .Update(ack => ack.Name)
                            .Include(ack => ack.Bar, bars =>
                            {
                                bars
                                    .On(bar => bar.Code)
                                    .Insert();
                            });
                    })
                    .ToMerge()
                    .ToString();

            Assert.True(true);
        }
    }
}