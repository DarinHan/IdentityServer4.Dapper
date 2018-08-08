using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace IdentityServer4.Dapper.Options
{
    public class DBProviderOptions
    {
        public string ConnectionString { get; set; }

        public DbProviderFactory DbProviderFactory { get; set; }

        public int CommandTimeOut { get; set; } = 3000;

        public string GetLastInsertID { get; set; }

        public Dictionary<string, string> ColumnProtect { get; set; }
    }
}
