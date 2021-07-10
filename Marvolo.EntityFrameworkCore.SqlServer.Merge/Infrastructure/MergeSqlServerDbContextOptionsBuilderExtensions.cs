using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Infrastructure
{
    public static class MergeSqlServerDbContextOptionsBuilderExtensions
    {
        public static SqlServerDbContextOptionsBuilder UseMerge(this SqlServerDbContextOptionsBuilder @this, Func<IServiceProvider, IMergeSourceLoader> loader, ServiceLifetime lifetime = default)
        {
            return @this.UseMerge(merge => merge.SourceLoader(loader, lifetime));
        }

        public static SqlServerDbContextOptionsBuilder UseMerge(this SqlServerDbContextOptionsBuilder @this, Action<MergeSqlServerDbContextOptionsBuilder> configure = default)
        {
            var builder = ((IRelationalDbContextOptionsBuilderInfrastructure) @this).OptionsBuilder;
            var extension = builder.Options.FindExtension<MergeSqlServerDbContextOptionsExtension>() ?? new MergeSqlServerDbContextOptionsExtension();

            ((IDbContextOptionsBuilderInfrastructure) builder).AddOrUpdateExtension(extension);

            configure?.Invoke(new MergeSqlServerDbContextOptionsBuilder(@this));

            return @this;
        }
    }
}