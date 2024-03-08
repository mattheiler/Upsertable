using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Upsertable.Abstractions;
using Upsertable.Data;

namespace Upsertable.SqlServer.Infrastructure;

public class SqlServerUpsertableDbContextOptionsExtension : IDbContextOptionsExtension
{
    private ExtensionInfo _info;
    private ServiceDescriptor _loader;
    private ServiceCollection _resolvers = new();

    public SqlServerUpsertableDbContextOptionsExtension()
    {
    }

    private SqlServerUpsertableDbContextOptionsExtension(SqlServerUpsertableDbContextOptionsExtension cloneable)
    {
        _loader = cloneable._loader;
    }

    public ExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        services.TryAdd(_loader);
        services.TryAdd(_resolvers);
        services.TryAddTransient<IDataTableFactory, DataTableFactory>();
    }

    public void Validate(IDbContextOptions options)
    {
        if (_loader == null) throw new InvalidOperationException("A default source load strategy is required.");
    }

    DbContextOptionsExtensionInfo IDbContextOptionsExtension.Info => Info;

    public SqlServerUpsertableDbContextOptionsExtension WithSourceLoader(Func<IServiceProvider, IDataTableLoader> factory)
    {
        var clone = Clone();
        clone._loader = new ServiceDescriptor(typeof(IDataTableLoader), factory, ServiceLifetime.Singleton);
        return clone;
    }

    public SqlServerUpsertableDbContextOptionsExtension WithDataResolver(Func<IServiceProvider, IDataResolver> factory)
    {
        var clone = Clone();
        clone._resolvers.Add(new ServiceDescriptor(typeof(IDataResolver), factory, ServiceLifetime.Singleton));
        return clone;
    }

    private SqlServerUpsertableDbContextOptionsExtension Clone()
    {
        var resolvers = new ServiceCollection();

        foreach (var resolver in _resolvers) resolvers.Add(resolver);

        var extension = new SqlServerUpsertableDbContextOptionsExtension(this)
        {
            _loader = _loader,
            _resolvers = resolvers
        };
        return extension;
    }

    public class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        private string _logFragment;

        internal ExtensionInfo(IDbContextOptionsExtension extension) : base(extension)
        {
        }

        private new SqlServerUpsertableDbContextOptionsExtension Extension => (SqlServerUpsertableDbContextOptionsExtension)base.Extension;

        public override bool IsDatabaseProvider => false;

        public override string LogFragment => _logFragment ??= GetLogFragment();

        public override int GetServiceProviderHashCode()
        {
            return 0;
        }

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
        {
            return true;
        }

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["Merge:" + nameof(SqlServerUpsertableDbContextOptionsBuilder.SourceLoader)] = (Extension._loader?.GetHashCode() ?? 0L).ToString(CultureInfo.InvariantCulture);
        }

        private string GetLogFragment()
        {
            var builder = new StringBuilder();

            if (Extension._loader != null)
                builder.Append("CommandTimeout=").Append(Extension._loader).Append(' ');

            return builder.ToString();
        }
    }
}