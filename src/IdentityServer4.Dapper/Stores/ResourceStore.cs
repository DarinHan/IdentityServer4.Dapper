using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IdentityServer4.Dapper.Interfaces;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace IdentityServer4.Dapper.Stores
{
    public class ResourceStore : IResourceStore
    {
        private readonly IApiResourceProvider _apiResource;
        private readonly IIdentityResourceProvider _identityResource;
        private readonly ILogger<ResourceStore> _logger;
        private readonly IConfiguration _configuration;

        public ResourceStore(IConfiguration configuration, IApiResourceProvider apiResource, IIdentityResourceProvider identityResource, ILogger<ResourceStore> logger)
        {
            this._configuration = configuration;
            this._apiResource = apiResource ?? throw new ArgumentNullException(nameof(apiResource));
            this._identityResource = identityResource ?? throw new ArgumentNullException(nameof(identityResource));
            this._logger = logger;
        }

        public Task<ApiResource> FindApiResourceAsync(string name)
        {
            var api = _apiResource.FindApiResource(name);
            if (api != null)
            {
                _logger.LogDebug("Found {api} API resource in database", name);
            }
            else
            {
                _logger.LogDebug("Did not find {api} API resource in database", name);
            }

            return Task.FromResult(api);
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var models = _apiResource.FindApiResourcesByScope(scopeNames.ToList()).AsEnumerable();
            _logger.LogDebug("Found {scopes} API scopes in database", models.SelectMany(x => x.Scopes).Select(x => x.Name));
            return Task.FromResult(models);
        }

        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var results = _identityResource.FindIdentityResourcesByScope(scopeNames).AsEnumerable();
            _logger.LogDebug("Found {scopes} identity scopes in database", results.Select(x => x.Name));

            return Task.FromResult(results);
        }

        /// <summary>
        /// do not support the all resource query default
        /// </summary>
        /// <returns></returns>
        public Task<Resources> GetAllResourcesAsync()
        {
            Resources result = null;
            if (_configuration["ShowAllResources"] == "true")
            {
                var apis = _apiResource.FindApiResourcesAll().AsEnumerable();
                var identities = _identityResource.FindIdentityResourcesAll().AsEnumerable();
                result = new Resources(identities, apis);
                _logger.LogDebug("Found {scopes} as all scopes in database", result.IdentityResources.Select(x => x.Name).Union(result.ApiResources.SelectMany(x => x.Scopes).Select(x => x.Name)));
            }
            else
            {
                result = new Resources();
            }
            return Task.FromResult(result);
        }
    }
}
