using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Upsertable.Infrastructure.Extensions;
using Upsertable.SqlBulkCopy.Infrastructure.Extensions;
using Upsertable.SqlDataAdapter.Extensions;
using Upsertable.Tests.Entities;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Upsertable.Tests;

public class Test
{
    private readonly TestDbContext _context;

    public Test()
    {
        var services = new ServiceCollection();

        services.AddDbContext<TestDbContext>(db => db.UseSqlServer("Server=.,61805;Database=Test;User Id=SA;Password=YourStrong@Passw0rd;TrustServerCertificate=True;Encrypt=False", sql => sql.UseUpsertable(merge => merge.UseSqlBulkCopy())));

        var provider = services.BuildServiceProvider();
        var context = provider.GetService<TestDbContext>();

        _context = context;
    }

    [Fact]
    public async Task Test1()
    {
        var sample = new Foo
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
        };

        await _context.Foos.AddAsync(sample);
        await _context.SaveChangesAsync();

        var statement = _context
            .Merge([sample])
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

        var result = await _context.Foos
            .Include(foo => foo.Fubs).ThenInclude(fub => fub.Baz).ThenInclude(baz => baz.Qux).ThenInclude(qux => qux.Fums)
            .SingleAsync(foo => foo.Id == sample.Id);

        Assert.Equal(sample.Code, result.Code);
        Assert.Single(result.Fubs);
        Assert.Equal(sample.Fubs.ElementAt(0).Baz.Code, result.Fubs.ElementAt(0).Baz.Code);
        Assert.Equal(sample.Fubs.ElementAt(0).Baz.Qux.Code, result.Fubs.ElementAt(0).Baz.Qux.Code);
        Assert.Single(result.Fubs.ElementAt(0).Baz.Qux.Fums);
        Assert.Equal(sample.Fubs.ElementAt(0).Baz.Qux.Fums.ElementAt(0).Code, result.Fubs.ElementAt(0).Baz.Qux.Fums.ElementAt(0).Code);
    }
}