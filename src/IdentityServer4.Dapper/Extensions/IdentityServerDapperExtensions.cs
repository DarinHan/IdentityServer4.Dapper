using IdentityServer4.Dapper.Options;
using IdentityServer4.Dapper.Services;
using IdentityServer4.Dapper.Stores;
using IdentityServer4.Dapper.Host;
using IdentityServer4.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer4.Dapper.Extensions
{
    /// <summary>
    /// Extension methods to add Dapper support to IdentityServer.
    /// </summary>
    public static class IdentityServerDapperExtensions
    {
        /// <summary>
        /// Configures Dapper implementation of IClientStore, IResourceStore, and ICorsPolicyService with IdentityServer.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="storeOptionsAction"></param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddConfigurationStore(this IIdentityServerBuilder builder, Action<ConfigurationStoreOptions> storeOptionsAction = null)
        {
            var options = new ConfigurationStoreOptions();
            builder.Services.AddSingleton(options);
            storeOptionsAction?.Invoke(options);

            builder.Services.AddTransient<Interfaces.IClientProvider, DefaultProviders.DefaultClientProvider>();
            builder.Services.AddTransient<Interfaces.IApiResourceProvider, DefaultProviders.DefaultApiResourceProvider>();
            builder.Services.AddTransient<Interfaces.IIdentityResourceProvider, DefaultProviders.DefaultIdentityResourceProvider>();

            builder.AddClientStore<ClientStore>();
            builder.AddResourceStore<ResourceStore>();
            builder.AddCorsPolicyService<CorsPolicyService>();
            return builder;
        }

        /// <summary>
        /// Configures Dapper implementation of IPersistedGrantStore with IdentityServer.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="storeOptionsAction"></param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddOperationalStore(this IIdentityServerBuilder builder, Action<OperationalStoreOptions> storeOptionsAction = null)
        {
            builder.Services.AddSingleton<TokenCleanup>();
            builder.Services.AddSingleton<IHostedService, TokenCleanupHost>();//auto clear expired tokens

            builder.Services.AddTransient<Interfaces.IPersistedGrantProvider, DefaultProviders.DefaultPersistedGrantProvider>();
            builder.Services.AddTransient<Interfaces.IPersistedGrantStoreClanup, DefaultProviders.DefaultPersistedGrantProvider>();

            var storeOptions = new OperationalStoreOptions();
            builder.Services.AddSingleton(storeOptions);
            storeOptionsAction?.Invoke(storeOptions);

            builder.Services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();

            return builder;
        }
    }
}
