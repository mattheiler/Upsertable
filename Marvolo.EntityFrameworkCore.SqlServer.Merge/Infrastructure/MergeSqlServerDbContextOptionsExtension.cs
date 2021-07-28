using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Marvolo.EntityFrameworkCore.SqlServer.Merge.Infrastructure
{
    public class MergeSqlServerDbContextOptionsExtension : IDbContextOptionsExtension
    {
        private ExtensionInfo _info;
        private ServiceDescriptor _loader;

        public MergeSqlServerDbContextOptionsExtension()
        {
        }

        private MergeSqlServerDbContextOptionsExtension(MergeSqlServerDbContextOptionsExtension cloneable)
        {
            _loader = cloneable._loader;
        }

        public ExtensionInfo Info => _info ??= new ExtensionInfo(this);

        public void ApplyServices(IServiceCollection services)
        {
            services.TryAdd(_loader);
            services.TryAddTransient<IMergeSourceBuilder, MergeSourceBuilder>();
        }

        public void Validate(IDbContextOptions options)
        {
            if (_loader == null) throw new InvalidOperationException("A default source load strategy is required.");
        }

        DbContextOptionsExtensionInfo IDbContextOptionsExtension.Info => Info;

        public MergeSqlServerDbContextOptionsExtension WithSourceLoader(Func<IServiceProvider, IMergeSourceLoader> factory, ServiceLifetime lifetime = default)
        {
            var clone = Clone();
            clone._loader = new ServiceDescriptor(typeof(IMergeSourceLoader), factory, lifetime);
            return clone;
        }

        private MergeSqlServerDbContextOptionsExtension Clone()
        {
            return new MergeSqlServerDbContextOptionsExtension(this);
        }

        public class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            private string _logFragment;

            internal ExtensionInfo(IDbContextOptionsExtension extension) : base(extension)
            {
            }

            private new MergeSqlServerDbContextOptionsExtension Extension => (MergeSqlServerDbContextOptionsExtension) base.Extension;

            public override bool IsDatabaseProvider => false;

            public override string LogFragment => _logFragment ??= GetLogFragment();

            public override long GetServiceProviderHashCode()
            {
                return 0;
            }

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
                debugInfo["Merge:" + nameof(MergeSqlServerDbContextOptionsBuilder.SourceLoader)] = (Extension._loader?.GetHashCode() ?? 0L).ToString(CultureInfo.InvariantCulture);
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
}