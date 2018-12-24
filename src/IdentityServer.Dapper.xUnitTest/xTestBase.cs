using MySqlN = IdentityServer4.Dapper.Extensions.MySql;
using MSSqlN = IdentityServer4.Dapper.Extensions.MSSql;
using IdentityServer4.Dapper.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Dapper.xUnitTest
{
    class xTestBase
    {
        public const string MSSQL = "MSSQL";
        public const string MySQL = "MySQL";

        public static DBProviderOptions GetDBProviderOptions(string type)
        {
            DBProviderOptions options = null;
            if (type == MySQL)
            {
                options = MySqlN.IdentityServerDapperDBExtensions.GetDefaultOptions();
                options.ConnectionString = "server=10.40.0.190;uid=changyin.han;pwd=fjfhhan07;database=identityserver4dev;SslMode=None;";
            }
            else if (type == MSSQL)
            {
                options = MSSqlN.IdentityServerDapperDBExtensions.GetDefaultOptions();
                options.ConnectionString = "server=10.40.0.190;uid=sa;pwd=Onesmart190;database=identityserver4;";
            }

            return options;
        }
    }
}
