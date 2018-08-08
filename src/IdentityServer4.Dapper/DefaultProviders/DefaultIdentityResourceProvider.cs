using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;
using Dapper;
using IdentityServer4.Dapper.Interfaces;
using IdentityServer4.Dapper.Mappers;
using IdentityServer4.Dapper.Options;
using IdentityServer4.Models;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Dapper.DefaultProviders
{
    class DefaultIdentityResourceProvider : IIdentityResourceProvider
    {
        private DBProviderOptions _options;
        private readonly ILogger<DefaultIdentityResourceProvider> _logger;

        public DefaultIdentityResourceProvider(DBProviderOptions dBProviderOptions, ILogger<DefaultIdentityResourceProvider> logger)
        {
            this._options = dBProviderOptions ?? throw new ArgumentNullException(nameof(dBProviderOptions));
            this._logger = logger;
        }

        public IEnumerable<IdentityResource> FindIdentityResourcesAll()
        {
            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;
                var claims = connection.Query<Entities.IdentityClaim, Entities.IdentityResource, Entities.IdentityClaim>("select * from IdentityClaims claim inner join IdentityResources identity on claim.auto_incrementResourceId = identity.id", (claim, indetity) => { claim.IdentityResource = indetity; return claim; }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                var identities = claims?.Select(c => c.IdentityResource).Distinct();
                if (identities != null)
                {
                    foreach (var item in identities)
                    {
                        item.UserClaims = claims?.Where(c => c.IdentityResource.Id == item.Id).AsList();
                    }
                }
                return identities.Select(c => c.ToModel());
            }
        }

        public IEnumerable<IdentityResource> FindIdentityResourcesByScope(IEnumerable<string> scopeNames)
        {
            if (scopeNames == null || scopeNames.Count() == 0)
            {
                return null;
            }

            if (scopeNames.Count() > 20)
            {
                var lstall = FindIdentityResourcesAll();
                return lstall.Where(c => scopeNames.Contains(c.Name));
            }
            else
            {
                using (var connection = _options.DbProviderFactory.CreateConnection())
                {
                    string condition = string.Concat("('", string.Join("','", scopeNames.ToArray()), "')");
                    connection.ConnectionString = _options.ConnectionString;
                    var claims = connection.Query<Entities.IdentityClaim, Entities.IdentityResource, Entities.IdentityClaim>("select * from IdentityClaims claim inner join IdentityResources identity on claim.auto_incrementResourceId = identity.id where identity.Name in @Condition", (claim, indetity) => { claim.IdentityResource = indetity; return claim; }, new { Condition = condition }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                    var identities = claims?.Select(c => c.IdentityResource).Distinct();
                    if (identities != null)
                    {
                        foreach (var item in identities)
                        {
                            item.UserClaims = claims?.Where(c => c.IdentityResource.Id == item.Id).AsList();
                        }
                    }
                    return identities.Select(c => c.ToModel());
                }
            }
        }
    }
}
