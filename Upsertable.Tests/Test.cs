using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upsertable.Infrastructure.Extensions;
using Upsertable.SqlBulkCopy.Infrastructure.Extensions;
using Upsertable.SqlDataAdapter.Extensions;
using Upsertable.Tests.Entities;
using Xunit;

namespace Upsertable.Tests;

public class Test
{
    private readonly TestDbContext _context;

    public Test()
    {
        var services = new ServiceCollection();

        services.AddDbContext<TestDbContext>(db => db.UseSqlServer("Server=.,61805;Database=Test;User Id=SA;Password=YourStrong@Passw0rd;TrustServerCertificate=True;Encrypt=False", sql => sql.UseUpsertable(merge => SqlBulkCopySqlServerUpsertableDbContextOptionsBuilderExtensions.UseSqlBulkCopy(merge))));

        var provider = services.BuildServiceProvider();
        var context = provider.GetService<TestDbContext>();

        _context = context;
    }

    [Fact]
    public async Task Test1()
    {
        var foos = new[]
        {
            new Foo
            {
                Code = Guid.NewGuid().ToString(),
                Fubs =
                {
                    new Fub
                    {
                        Baz = new Baz
                        {
                            Code = Guid.NewGuid().ToString(),
                            Qux = new Qux
                            {
                                Code = Guid.NewGuid().ToString(),
                                Fums =
                                {
                                    new Fum
                                    {
                                        Code = Guid.NewGuid().ToString()
                                    }
                                }
                            }
                        }
                    }
                }
            },
            new Foo
            {
                Code = Guid.NewGuid().ToString(),
                Fubs =
                {
                    new Fub
                    {
                        Baz = new Baz
                        {
                            Code = Guid.NewGuid().ToString(),
                            Qux = new Qux
                            {
                                Code = Guid.NewGuid().ToString(),
                                Fums =
                                {
                                    new Fum
                                    {
                                        Code = Guid.NewGuid().ToString()
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var statement = _context
            .Merge(foos)
            .On(foo => foo.Code)
            .Insert()
            .Update(foo => foo.Name)
            .MergeMany(foo => foo.Fubs, fubs =>
            {
                fubs
                    .Insert()
                    .Merge(fub => fub.Baz, bazs =>
                    {
                        bazs
                            .On(baz => baz.Code)
                            .Insert()
                            .Merge(baz => baz.Qux, quxs =>
                            {
                                quxs
                                    .On(fum => fum.Code)
                                    .Insert()
                                    .MergeMany(qux => qux.Fums, fums =>
                                    {
                                        fums
                                            .On(fum => fum.Code)
                                            .Insert()
                                            .UsingSqlDataAdapter();
                                    });
                            });
                    });
            })
            .MergeMany(foo => foo.Acks, acks =>
            {
                acks
                    .On(bar => bar.Code)
                    .Insert()
                    .Update(ack => ack.Name)
                    .Merge(ack => ack.Bar, bars =>
                    {
                        bars
                            .On(bar => bar.Code)
                            .Insert();
                    });
            })
            .ToMerge();

        await statement.ExecuteAsync();
        await statement.ExecuteAsync();

        Assert.True(true);
    }
}