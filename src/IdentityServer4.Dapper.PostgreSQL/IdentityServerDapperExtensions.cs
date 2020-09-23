using IdentityServer4.Dapper.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using Npgsql;
using System.Linq;

namespace IdentityServer4.Dapper.Extensions.PostgreSQL
{
    public static class IdentityServerDapperDBExtensions
    {
        public static IIdentityServerBuilder AddPostgreSQLProvider(this IIdentityServerBuilder builder, Action<DBProviderOptions> dbProviderOptionsAction = null)
        {
            var options = GetDefaultOptions();
            dbProviderOptionsAction?.Invoke(options);
            builder.Services.AddSingleton(options);
            return builder;
        }

        public static DBProviderOptions GetDefaultOptions()
        {
            //config mysql
            var options = new DBProviderOptions();
            options.DbProviderFactory = NpgsqlFactory.Instance;
            //get last insert id for insert actions
            options.GetLastInsertID = "select LASTVAL();";
            //config the ColumnName protect string, postgresql using ""
            options.ColumnProtect = new System.Collections.Generic.Dictionary<string, string>();
            options.ColumnProtect.Add("left", "");
            options.ColumnProtect.Add("right", "");
            options.GetInArray = " = ANY ";
            //add singgleton
            options.GetPageQuerySQL = (input, pageindex, pagesize, totalcount, orderby, pairs) =>
            {
                string limitsql = string.Empty;
                if (pagesize > 0)
                {
                    if (pagesize > totalcount)
                    {
                        pagesize = totalcount;
                    }
                    pairs.Add("start", (pageindex - 1) * pagesize);
                    pairs.Add("size", pagesize);
                    limitsql = "limit @size offset @start";
                }

                if (input.IndexOf("order by", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    orderby = "";
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(orderby) && orderby.IndexOf("order by", StringComparison.CurrentCultureIgnoreCase) < 0)
                    {
                        orderby = "order by " + orderby;
                    }
                }

                return $"{input} {orderby} {limitsql}";
            };
            return options;
        }
    }
}
