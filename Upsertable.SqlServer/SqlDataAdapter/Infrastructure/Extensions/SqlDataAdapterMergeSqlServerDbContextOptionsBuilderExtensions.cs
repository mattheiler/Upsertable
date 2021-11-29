﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Upsertable.Abstractions;
using Upsertable.SqlServer.Infrastructure;

namespace Upsertable.SqlServer.SqlDataAdapter.Infrastructure.Extensions
{
    public static class SqlDataAdapterMergeSqlServerDbContextOptionsBuilderExtensions
    {
        public static MergeSqlServerDbContextOptionsBuilder UseSqlDataAdapter(this MergeSqlServerDbContextOptionsBuilder @this, Action<SqlDataAdapterDataTableLoaderOptions> configure = default)
        {
            return @this.SourceLoader(SqlDataAdapterMergeSourceLoaderFactory(configure));
        }

        public static MergeSqlServerDbContextOptionsBuilder UseSqlDataAdapter(this MergeSqlServerDbContextOptionsBuilder @this, ServiceLifetime lifetime, Action<SqlDataAdapterDataTableLoaderOptions> configure = default)
        {
            return @this.SourceLoader(SqlDataAdapterMergeSourceLoaderFactory(configure), lifetime);
        }

        private static Func<IServiceProvider, IDataTableLoader> SqlDataAdapterMergeSourceLoaderFactory(Action<SqlDataAdapterDataTableLoaderOptions> configure)
        {
            return provider =>
            {
                var options = new SqlDataAdapterDataTableLoaderOptions();
                configure?.Invoke(options);
                return new SqlDataAdapterDataTableLoader(options);
            };
        }
    }
}