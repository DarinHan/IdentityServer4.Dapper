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
                    var secrets = GetSecretByAPIID(api.Id);
                    if (secrets != null)
                    {
                        foreach (var item in secrets)
                        {
                            item.ApiResource = api;
                        }
                        api.Secrets = secrets.AsList();
                    }
                    var scopes = GetScopesByAPIID(api.Id);
                    if (scopes != null)
                    {
                        foreach (var item in scopes)
                        {
                            item.ApiResource = api;
                        }
                        api.Scopes = scopes.AsList();
                    }
                    var apiclaims = GetClaimsByAPIID(api.Id);
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

        public Entities.ApiResource GetByName(string name)
        {
            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;

                var api = connection.QueryFirstOrDefault<Entities.ApiResource>("select * from ApiResources where Name = @Name", new { Name = name }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                return api;
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



        public void Add(ApiResource apiResource)
        {
            var dbapiResource = FindApiResource(apiResource.Name);
            if (dbapiResource != null)
            {
                throw new InvalidOperationException($"you can not add an existed ApiResource,Name={apiResource.Name}.");
            }

            var entity = apiResource.ToEntity();
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                using (var t = con.BeginTransaction())
                {
                    try
                    {
                        string left = _options.ColumnProtect["left"];
                        string right = _options.ColumnProtect["right"];

                        var apiid = con.ExecuteScalar<int>($"insert into ApiResources ({left}Description{right},DisplayName,Enabled,{left}Name{right}) values (@Description,@DisplayName,@Enabled,@Name);{_options.GetLastInsertID}", new
                        {
                            entity.Description,
                            entity.DisplayName,
                            entity.Enabled,
                            entity.Name
                        }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);

                        var ret = 0;
                        if (entity.UserClaims != null && entity.UserClaims.Count() > 0)
                        {
                            foreach (var item in entity.UserClaims)
                            {
                                ret = con.Execute($"insert into ApiClaims (ApiResourceId,{left}Type{right}) values (@ApiResourceId,@Type)", new
                                {
                                    ApiResourceId = apiid,
                                    item.Type
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        if (entity.Secrets != null && entity.Secrets.Count() > 0)
                        {
                            foreach (var item in entity.Secrets)
                            {
                                ret = con.Execute($"insert into ApiSecrets (ApiResourceId,{left}Description{right},Expiration,{left}Type{right},{left}Value{right}) values (@ApiResourceId,@Description,@Expiration,@Type,@Value)", new
                                {
                                    ApiResourceId = apiid,
                                    item.Description,
                                    item.Expiration,
                                    item.Type,
                                    item.Value
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        if (entity.Scopes != null && entity.Scopes.Count() > 0)
                        {
                            foreach (var item in entity.Scopes)
                            {
                                ret = con.Execute($"insert into ApiScopes (ApiResourceId,{left}Description{right},DisplayName,Emphasize,{left}Name{right},{left}Required{right},ShowInDiscoveryDocument) values (@ApiResourceId,@Description,@DisplayName,@Emphasize,@Name,@Required,@ShowInDiscoveryDocument)", new
                                {
                                    ApiResourceId = apiid,
                                    item.Description,
                                    item.DisplayName,
                                    item.Emphasize,
                                    item.Name,
                                    item.Required,
                                    item.ShowInDiscoveryDocument
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public void Remove(string name)
        {
            var entity = GetByName(name);
            if (entity == null)
            {
                return;
            }
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                var t = con.BeginTransaction();
                try
                {
                    var ret = con.Execute($"delete from ApiResources where Id=@Id;", new
                    {
                        entity.Id
                    }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                    if (ret != 1)
                    {
                        throw new Exception($"execute delete error,return values is {ret}");
                    }

                    con.Execute($"delete from ApiClaims where ApiResourceId=@ApiResourceId;delete from ApiSecrets where ApiResourceId=@ApiResourceId;delete from ApiScopes where ApiResourceId=@ApiResourceId;", new
                    {
                        ApiResourceId = entity.Id
                    }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw ex;
                }
                finally
                {
                    con.Close();
                }
            }
        }

        #region 子属性

        public IEnumerable<Entities.ApiSecret> GetSecretByAPIID(int apiresourceid)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;

                return con.Query<Entities.ApiSecret>("select * from apisecrets where ApiResourceId = @ApiResourceId", new { ApiResourceId = apiresourceid }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
            }
        }
        public IEnumerable<Entities.ApiScope> GetScopesByAPIID(int apiresourceid)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                var scopeclaims = con.Query<Entities.ApiScope, Entities.ApiScopeClaim, Entities.ApiScopeClaim>("select * from ApiScopes inner join ApiScopeClaims on ApiScopes.id = ApiScopeClaims.ApiScopeId where ApiResourceId = @ApiResourceId", (apiscope, apiscopeclaim) =>
                 {
                     apiscopeclaim.ApiScope = apiscope;
                     return apiscopeclaim;
                 }, new { ApiResourceId = apiresourceid }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                var scopes = scopeclaims.Select(c => c.ApiScope).Distinct();
                foreach (var scope in scopes)
                {
                    scope.UserClaims = scopeclaims.Where(c => c.ApiScope.Id == scope.Id).ToList();
                }
                return scopes;
            }
        }

        public IEnumerable<Entities.ApiResourceClaim> GetClaimsByAPIID(int apiresourceid)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return con.Query<Entities.ApiResourceClaim>("select * from ApiClaims where ApiResourceId = @ApiResourceId", new { ApiResourceId = apiresourceid }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
            }
        }
        #endregion

        public IEnumerable<ApiResource> Search(string keywords, int pageIndex, int pageSize, out int totalCount)
        {
            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;

                DynamicParameters pairs = new DynamicParameters();
                pairs.Add("keywords", "%" + keywords + "%");

                var countsql = "select count(1) from ApiResources where Name like @keywords or DisplayName like @keywords or Description like @keywords";
                totalCount = connection.ExecuteScalar<int>(countsql, pairs, commandType: CommandType.Text);

                if (totalCount == 0)
                {
                    return null;
                }

                var apis = connection.Query<Entities.ApiResource>(_options.GetPageQuerySQL("select * from ApiResources where Name like @keywords or DisplayName like @keywords or Description like @keywords", pageIndex, pageSize, totalCount, "", pairs), pairs, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                if (apis != null)
                {
                    return apis.Select(c => c.ToModel());
                }
                return null;
            }
        }
    }
}
