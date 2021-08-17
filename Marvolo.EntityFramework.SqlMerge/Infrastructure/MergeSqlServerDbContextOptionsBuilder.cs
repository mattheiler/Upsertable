﻿using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Marvolo.EntityFramework.SqlMerge.Infrastructure
{
    public class MergeSqlServerDbContextOptionsBuilder : IMergeSqlServerDbContextOptionsInfrastructure
    {
        private readonly SqlServerDbContextOptionsBuilder _optionsBuilder;

        public MergeSqlServerDbContextOptionsBuilder(SqlServerDbContextOptionsBuilder optionsBuilder)
        {
            _optionsBuilder = optionsBuilder;
        }

        SqlServerDbContextOptionsBuilder IMergeSqlServerDbContextOptionsInfrastructure.OptionsBuilder => _optionsBuilder;

        public MergeSqlServerDbContextOptionsBuilder SourceLoader(Func<IServiceProvider, IMergeSourceLoader> factory, ServiceLifetime lifetime = default)
        {
            return WithOption(e => e.WithSourceLoader(factory, lifetime));
        }

        private MergeSqlServerDbContextOptionsBuilder WithOption(Func<MergeSqlServerDbContextOptionsExtension, MergeSqlServerDbContextOptionsExtension> configure)
        {
            var relational = ((IRelationalDbContextOptionsBuilderInfrastructure) _optionsBuilder).OptionsBuilder;
            var extension = relational.Options.FindExtension<MergeSqlServerDbContextOptionsExtension>() ?? new MergeSqlServerDbContextOptionsExtension();

            ((IDbContextOptionsBuilderInfrastructure) relational).AddOrUpdateExtension(configure(extension));

            return this;
        }
    }
}