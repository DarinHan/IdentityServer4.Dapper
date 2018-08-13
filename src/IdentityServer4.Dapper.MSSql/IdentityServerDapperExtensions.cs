using IdentityServer4.Dapper.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data.SqlClient;

namespace IdentityServer4.Dapper.MSSql
{
    public static class IdentityServerDapperExtensions
    {
        public static IIdentityServerBuilder AddMSSQLProvider(this IIdentityServerBuilder builder, Action<DBProviderOptions> dbProviderOptionsAction = null)
        {
            //config mssql
            var options = new DBProviderOptions();
            options.DbProviderFactory = SqlClientFactory.Instance;
            //get last insert id for insert actions
            options.GetLastInsertID = "select @@IDENTITY";
            //config the ColumnName protect string, mssql using "[]"
            options.ColumnProtect = new System.Collections.Generic.Dictionary<string, string>();
            options.ColumnProtect.Add("left", "[");
            options.ColumnProtect.Add("right", "]");
            //add singgleton
            builder.Services.AddSingleton(options);
            dbProviderOptionsAction?.Invoke(options);
            return builder;
        }
    }
}
