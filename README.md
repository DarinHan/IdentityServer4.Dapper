# IdentityServer4.Dapper

IdentityServer4.Dapper is a package that provides the configuration APIs to add persistence for IdentityServer 4 configuration data that uses Dapper as its database abstraction.

You can use all database Dapper supported easily.

what you need to do is download the package Darin.IdentityServer4.Dapper.Mysql from nuget using 
Install-Package Darin.IdentityServer4.Dapper.Mysql -Version 1.0.0 

then using the AddMySQLProvider meth to config the DB Connection, then call the AddConfigurationStore and AddOperationalStore method to config the stores provided in this solution.
<code>
public IServiceProvider ConfigureServices(IServiceCollection services)
{
    services.AddIdentityServer()
        .AddDeveloperSigningCredential()
        //use mysql provider
        .AddMySQLProvider(option =>
        {
            option.ConnectionString = "server=127.0.0.1;uid=darinhan;pwd=darinhanpassword;database=identityserver4;SslMode=None;";
        })
        // configure identity server with default stores, keys, clients and scopes,which use the standard SQL
        .AddConfigurationStore()
        // configure identity server with default Operationalstores,which use the standard SQL
        .AddOperationalStore(option =>
        {
            option.EnableTokenCleanup = true;
            option.TokenCleanupInterval = 10;
        });

    return services.BuildServiceProvider(validateScopes: true);
}
</code>

notice that, we provide some default Providers in namespace IdentityServer4.Dapper.DefaultProviders, these providers can work all right indepence of which rmdb used. In other words, all sql used in IdentityServer4.Dapper.DefaultProviders are standard SQL.Maybe some problems are made in your project, you can override it in subclass and remove from servicecollection.

