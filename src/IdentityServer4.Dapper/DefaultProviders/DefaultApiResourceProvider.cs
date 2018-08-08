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
    public class DefaultApiResourceProvider : IApiResourceProvider
    {
        private DBProviderOptions _options;
        private readonly ILogger<DefaultApiResourceProvider> _logger;

        public DefaultApiResourceProvider(DBProviderOptions dBProviderOptions, ILogger<DefaultApiResourceProvider> logger)
        {
            this._options = dBProviderOptions ?? throw new ArgumentNullException(nameof(dBProviderOptions));
            this._logger = logger;
        }

        public ApiResource FindApiResource(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;

                var api = connection.QueryFirstOrDefault<Entities.ApiResource>("select * from ApiResources where Name = @Name", new { Name = name }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                if (api != null)
                {
                    var secrets = connection.Query<Entities.ApiSecret>("select * from ApiSecrets where ApiResourceId = @ApiResourceId", new { ApiResourceId = api.Id }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                    if (secrets != null)
                    {
                        foreach (var item in secrets)
                        {
                            item.ApiResource = api;
                        }
                        api.Secrets = secrets.AsList();
                    }
                    var scopes = connection.Query<Entities.ApiScope>("select * from ApiScopes where ApiResourceId = @ApiResourceId", new { ApiResourceId = api.Id }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                    if (scopes != null)
                    {
                        foreach (var item in scopes)
                        {
                            var claims = connection.Query<Entities.ApiScopeClaim>("select * from ApiScopeClaims where ApiScopeId = @ApiScopeId", new { ApiScopeId = item.Id }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                            if (claims != null)
                            {
                                item.UserClaims = claims.AsList();
                            }
                            item.ApiResource = api;
                        }
                        api.Scopes = scopes.AsList();
                    }
                    var apiclaims = connection.Query<Entities.ApiResourceClaim>("select * from ApiClaims where ApiResourceId = @ApiResourceId", new { ApiResourceId = api.Id }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                    if (apiclaims != null)
                    {
                        foreach (var item in apiclaims)
                        {
                            item.ApiResource = api;
                        }
                        api.UserClaims = apiclaims.AsList();
                    }
                }

                return api?.ToModel();
            }
        }

        public IEnumerable<ApiResource> FindApiResourcesAll()
        {
            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;

                var apilist = connection.Query<Entities.ApiResource>("select * from ApiResources", commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                if (apilist != null && apilist.Count() > 0)
                {
                    var secrets = connection.Query<Entities.ApiSecret, Entities.ApiResource, Entities.ApiSecret>("select * from ApiSecrets sec inner join ApiResources api on api.id = sec.ApiResourceId", (sec, api) => { sec.ApiResource = api; return sec; }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, splitOn: "id");

                    var scopes = connection.Query<Entities.ApiScope, Entities.ApiResource, Entities.ApiScope>("select * from ApiScopes scope inner join ApiResources api on api.id = scope.ApiResourceId", (scope, api) => { scope.ApiResource = api; return scope; }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);

                    var claims = connection.Query<Entities.ApiScopeClaim, Entities.ApiScope, Entities.ApiScopeClaim>("select * from ApiScopeClaims claim inner join ApiScopes scope on scope.id = claim.ApiScopeId", (claim, scope) => { claim.ApiScope = scope; return claim; }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);


                    var apiclaims = connection.Query<Entities.ApiResourceClaim, Entities.ApiResource, Entities.ApiResourceClaim>("select * from ApiClaims claim inner join ApiResources api on api.id = claim.ApiResourceId", (claim, api) => { claim.ApiResource = api; return claim; }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);

                    connection.Close(); //提前关闭连接

                    //处理数据
                    if (scopes != null && scopes.Count() > 0)
                    {
                        foreach (var scope in scopes)
                        {
                            scope.UserClaims = claims?.Where(c => c.ApiScope.Id == scope.Id).AsList();
                        }
                    }
                    foreach (var item in apilist)
                    {
                        item.Secrets = secrets?.Where(c => c.ApiResource.Id == item.Id).AsList();
                        item.Scopes = scopes?.Where(c => c.ApiResource.Id == item.Id).AsList();
                        item.UserClaims = apiclaims?.Where(c => c.ApiResource.Id == item.Id).AsList();
                    }
                }

                return apilist.Select(c => c.ToModel());
            }
        }

        public IEnumerable<ApiResource> FindApiResourcesByScope(IEnumerable<string> scopeNames)
        {
            if (scopeNames == null || scopeNames.Count() == 0)
            {
                return null;
            }

            var names = scopeNames.ToArray();
            var lstall = FindApiResourcesAll();
            return lstall.Where(c => c.Scopes.Where(s => names.Contains(s.Name)).Any()).AsEnumerable();
        }
    }
}
