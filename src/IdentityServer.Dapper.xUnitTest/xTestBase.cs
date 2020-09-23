using MySqlN = IdentityServer4.Dapper.Extensions.MySql;
using MSSqlN = IdentityServer4.Dapper.Extensions.MSSql;
using PostgreSqlN = IdentityServer4.Dapper.Extensions.PostgreSQL;
using IdentityServer4.Dapper.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdentityServer.Dapper.xUnitTest
{
    class xTestBase
    {
        public const string MSSQL = "MSSQL";
        public const string MySQL = "MySQL";
        public const string PostgreSQL = "PostgreSQL";

        public static DBProviderOptions GetDBProviderOptions(string type)
        {
            DBProviderOptions options = null;
            if (type == MySQL)
            {
                options = MySqlN.IdentityServerDapperDBExtensions.GetDefaultOptions();
                //options.ConnectionString = "server=10.40.0.190;uid=changyin.han;pwd=fjfhhan07;database=identityserver4dev;SslMode=None;";
                options.ConnectionString = "server=localhost;uid=idadmin;pwd=admin123;database=identityserver;SslMode=None;";
            }
            else if (type == MSSQL)
            {
                options = MSSqlN.IdentityServerDapperDBExtensions.GetDefaultOptions();
                //options.ConnectionString = "server=10.40.0.190;uid=sa;pwd=Onesmart190;database=identityserver4;";
                options.ConnectionString = "server=localhost;uid=sa;pwd=Saiahcsep2020@;database=identityserver;";
            }
            else if (type == PostgreSQL)
            {
                options = PostgreSqlN.IdentityServerDapperDBExtensions.GetDefaultOptions();
                options.ConnectionString = "Host=localhost;Port=32676;Username=postgresadmin;Password=admin123;Database=postgresdb;Minimum Pool Size=5;Search Path=identityserver";
            }

            return options;
        }

        public static IDistributedCache GetCache()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddDistributedMemoryCache();

            var provider = services.BuildServiceProvider();
            var cache = provider.GetService<IDistributedCache>();
            return cache;
        }
    }
}
