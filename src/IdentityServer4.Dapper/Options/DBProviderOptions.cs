using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace IdentityServer4.Dapper.Options
{
    public class DBProviderOptions
    {
        /// <summary>
        /// connection string for dapper
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// DBProvider Factory in ADO
        /// </summary>
        public DbProviderFactory DbProviderFactory { get; set; }

        /// <summary>
        /// time out setting
        /// </summary>
        public int CommandTimeOut { get; set; } = 3000;

        /// <summary>
        ///  sql for get new id inserted
        /// </summary>
        public string GetLastInsertID { get; set; }

        /// <summary>
        /// column specified in each db
        /// </summary>
        public Dictionary<string, string> ColumnProtect { get; set; }

        /// <summary>
        /// func for get paged sql
        /// Func string,int, int, int, string, DynamicParameters,string
        /// string:original sql for select
        /// int:pageindex
        /// int:pagesize
        /// int:totalcount
        /// string:order by
        /// DynamicParameters:parameters for dapper
        /// string:paged sql returned
        /// </summary>
        public Func<string, int, int, int, string, DynamicParameters, string> GetPageQuerySQL { get; set; }
    }
}
