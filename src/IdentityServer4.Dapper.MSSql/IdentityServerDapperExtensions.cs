using IdentityServer4.Dapper.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data.SqlClient;

namespace IdentityServer4.Dapper.Extensions.MSSql
{
    public static class IdentityServerDapperDBExtensions
    {
        public static IIdentityServerBuilder AddMSSQLProvider(this IIdentityServerBuilder builder, Action<DBProviderOptions> dbProviderOptionsAction = null)
        {
            var options = GetDefaultOptions();
            dbProviderOptionsAction?.Invoke(options);
            builder.Services.AddSingleton(options);
            return builder;
        }
        public static DBProviderOptions GetDefaultOptions()
        {
            //config mssql
            var options = new DBProviderOptions();
            options.DbProviderFactory = SqlClientFactory.Instance;
            //get last insert id for insert actions
            options.GetLastInsertID = "select @@IDENTITY;";
            //config the ColumnName protect string, mssql using "[]"
            options.ColumnProtect = new System.Collections.Generic.Dictionary<string, string>();
            options.ColumnProtect.Add("left", "[");
            options.ColumnProtect.Add("right", "]");
            options.GetInArray = " in ";

            options.GetPageQuerySQL = (input, pageindex, pagesize, totalcount, orderby, pairs) =>
            {
                int pagestart = 0;
                int pageend = 0;
                string limitsql = string.Empty;
                if (pagesize > 0)
                {
                    if (pagesize > totalcount)
                    {
                        pagesize = totalcount;
                    }
                    pagestart = (pageindex - 1) * pagesize + 1;
                    pageend = pagestart - 1 + pagesize;
                }

                if (string.IsNullOrWhiteSpace(orderby))
                {
                    orderby = "order by id"; //default 
                }

                if (!string.IsNullOrWhiteSpace(orderby) && orderby.IndexOf("order by", StringComparison.CurrentCultureIgnoreCase) < 0)
                {
                    orderby = "order by " + orderby;
                }

                input = $"select ROW_NUMBER() over ({orderby}) as rowid,{input.Substring(input.IndexOf("select", StringComparison.CurrentCultureIgnoreCase) + 6)}";

                return $"select * from ({input}) as innertable where rowid between {pagestart} and {pageend};";
            };
            return options;
        }
    }
}
