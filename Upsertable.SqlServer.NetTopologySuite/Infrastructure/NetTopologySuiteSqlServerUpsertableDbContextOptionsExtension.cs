using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Upsertable.SqlServer.NetTopologySuite.Infrastructure
{
    public class NetTopologySuiteSqlServerUpsertableDbContextOptionsExtension : IDbContextOptionsExtension
    {
        private ExtensionInfo _info;

        public NetTopologySuiteSqlServerUpsertableDbContextOptionsExtension()
        {
        }

        private NetTopologySuiteSqlServerUpsertableDbContextOptionsExtension(NetTopologySuiteSqlServerUpsertableDbContextOptionsExtension cloneable)
        {
        }

        public ExtensionInfo Info => _info ??= new ExtensionInfo(this);

        public void ApplyServices(IServiceCollection services)
        {
        }

        public void Validate(IDbContextOptions options)
        {
        }

        DbContextOptionsExtensionInfo IDbContextOptionsExtension.Info => Info;

        private NetTopologySuiteSqlServerUpsertableDbContextOptionsExtension Clone()
        {
            return new NetTopologySuiteSqlServerUpsertableDbContextOptionsExtension(this);
        }

        public class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            private string _logFragment;

            internal ExtensionInfo(IDbContextOptionsExtension extension) : base(extension)
            {
            }

            private new NetTopologySuiteSqlServerUpsertableDbContextOptionsExtension Extension => (NetTopologySuiteSqlServerUpsertableDbContextOptionsExtension)base.Extension;

            public override bool IsDatabaseProvider => false;

            public override string LogFragment => _logFragment ??= GetLogFragment();

            public override long GetServiceProviderHashCode()
            {
                return 0;
            }

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
            }

            private string GetLogFragment()
            {
                var builder = new StringBuilder();

                return builder.ToString();
            }
        }
    }
}
