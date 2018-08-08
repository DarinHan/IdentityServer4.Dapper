using IdentityServer4.Dapper.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace IdentityServer4.Dapper.Extensions
{
    public static class IdentityServerDapperExtensions
    {
        public static IIdentityServerBuilder AddMySQLProvider(this IIdentityServerBuilder builder, Action<DBProviderOptions> dbProviderOptionsAction = null)
        {
            //config mysql
            var options = new DBProviderOptions();
            options.DbProviderFactory = new MySqlClientFactory();
            //get last insert id for insert actions
            options.GetLastInsertID = "select last_insert_id();"; 
            //config the ColumnName protect string, mysql using "`"
            options.ColumnProtect = new System.Collections.Generic.Dictionary<string, string>();
            options.ColumnProtect.Add("left", "`");
            options.ColumnProtect.Add("right", "`");
            //add singgleton
            builder.Services.AddSingleton(options); 
            dbProviderOptionsAction?.Invoke(options);
            return builder;
        }
    }
}
