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
using Microsoft.Extensions.Caching.Distributed;

namespace IdentityServer4.Dapper.DefaultProviders
{
    public class DefaultApiResourceProvider : IApiResourceProvider
    {
        private DBProviderOptions _options;
        private readonly ILogger<DefaultApiResourceProvider> _logger;
        private string left;
        private string right;

        private readonly IDistributedCache _cache;
        private static volatile object locker = new object();

        public DefaultApiResourceProvider(DBProviderOptions dBProviderOptions, ILogger<DefaultApiResourceProvider> logger, IDistributedCache cache)
        {
            this._options = dBProviderOptions ?? throw new ArgumentNullException(nameof(dBProviderOptions));
            this._logger = logger;
            left = _options.ColumnProtect["left"];
            right = _options.ColumnProtect["right"];
            _cache = cache;
        }

        #region Query
        public ApiResource FindApiResource(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var key = "apiresource." + name;
            var apimodel = _cache.Get<ApiResource>(key);
            if (apimodel == null)
            {
                lock (locker)
                {
                    apimodel = _cache.Get<ApiResource>(key);
                    if (apimodel != null)
                    {
                        return apimodel;
                    }
                    using (var connection = _options.DbProviderFactory.CreateConnection())
                    {
                        connection.ConnectionString = _options.ConnectionString;

                        var api = connection.QueryFirstOrDefault<Entities.ApiResource>("select * from ApiResources where Name = @Name", new { Name = name }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                        if (api != null)
                        {
                            var secrets = GetSecretByApiResourceId(api.Id);
                            if (secrets != null)
                            {
                                foreach (var item in secrets)
                                {
                                    item.ApiResource = api;
                                }
                                api.Secrets = secrets.AsList();
                            }
                            var scopes = GetScopesByApiResourceId(api.Id);
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

                        apimodel = api?.ToModel();

                        if (apimodel != null)
                        {
                            _cache.Set<ApiResource>(key, apimodel, TimeSpan.FromHours(24));
                        }
                    }
                }
            }

            return apimodel;
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
        #endregion

        #region Add
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
                        entity.Id = apiid;

                        if (entity.UserClaims != null && entity.UserClaims.Count() > 0)
                        {
                            foreach (var item in entity.UserClaims)
                            {
                                InsertApiResourceClaim(item, entity.Id, con, t);
                            }
                        }
                        if (entity.Secrets != null && entity.Secrets.Count() > 0)
                        {
                            foreach (var item in entity.Secrets)
                            {
                                InsertApiSecretsByApiResourceId(item, apiid, con, t);
                            }
                        }
                        if (entity.Scopes != null && entity.Scopes.Count() > 0)
                        {
                            InsertApiScopeByApiResourceId(entity.Scopes, entity.Id, con, t);
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
        #endregion

        #region Remove
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
                    RemoveApiScopeByApiResourceId(entity.Id, con, t);
                    con.Execute($"delete from ApiClaims where ApiResourceId=@ApiResourceId;delete from ApiSecrets where ApiResourceId=@ApiResourceId;", new
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

            var key = "apiresource." + name;
            _cache.Remove(key);
        }
        #endregion

        #region Search
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
        #endregion

        #region Update
        public void Update(ApiResource apiResource)
        {

            var dbitem = GetByName(apiResource.Name);
            if (dbitem == null)
            {
                throw new InvalidOperationException($"you can not update an unexisted ApiResource,Name={apiResource.Name}.");
            }
            var entity = apiResource.ToEntity();
            entity.Id = dbitem.Id;
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                using (var t = con.BeginTransaction())
                {
                    try
                    {
                        var ret = con.Execute($"update ApiResources set {left}Description{right} = @Description," +
                            $"DisplayName=@DisplayName," +
                            $"Enabled=@Enabled," +
                            $"{left}Name{right}=@Name where Id=@Id;", entity, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);

                        UpdateScopesByApiResourceId(entity.Scopes, entity.Id, con, t);
                        UpdateApiSecretsByApiResourceId(entity.Secrets, entity.Id, con, t);
                        UpdateClaimsByApiResourceId(entity.UserClaims, entity.Id, con, t);
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
            }

            var key = "apiresource." + apiResource.Name;
            _cache.Remove(key);
        }
        #endregion

        #region 子属性
        #region ApiScope
        public IEnumerable<Entities.ApiScope> GetScopesByApiResourceId(int apiresourceid)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return GetScopesByApiResourceId(apiresourceid, con, null);
            }
        }

        public IEnumerable<Entities.ApiScope> GetScopesByApiResourceId(int apiresourceid, IDbConnection con, IDbTransaction t)
        {
            var scopes = con.Query<Entities.ApiScope>("select * from ApiScopes where ApiResourceId = @ApiResourceId", new { ApiResourceId = apiresourceid }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
            if (scopes != null && scopes.Count() > 0)
            {
                var scopeclaims = con.Query<Entities.ApiScopeClaim>("select ApiScopeClaims.* from ApiScopes inner join ApiScopeClaims on ApiScopes.id = ApiScopeClaims.ApiScopeId where ApiResourceId = @ApiResourceId", new { ApiResourceId = apiresourceid }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                if (!scopeclaims.IsEmpty())
                {
                    foreach (var scope in scopes)
                    {
                        scope.UserClaims = scopeclaims.Where(c => c.ApiScope.Id == scope.Id).ToList();
                    }
                }
            }
            return scopes;
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

        public void UpdateScopesByApiResourceId(ApiResource apiResource)
        {
            var dbitem = GetByName(apiResource.Name);
            if (dbitem == null)
            {
                throw new InvalidOperationException($"could not update ApiResource {apiResource.Name} not existed");
            }
            var entity = apiResource.ToEntity();
            entity.Id = dbitem.Id;
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                var t = con.BeginTransaction();

                try
                {
                    UpdateScopesByApiResourceId(entity.Scopes, entity.Id, con, t);
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw ex;
                }
            }
        }
        private void InsertApiScopeByApiResourceId(IEnumerable<Entities.ApiScope> apiScopes, int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            if (apiScopes.IsEmpty())
            {
                return;
            }
            foreach (var item in apiScopes)
            {
                var scopeid = con.ExecuteScalar<int>($"insert into ApiScopes (ApiResourceId,{left}Description{right},DisplayName,Emphasize,{left}Name{right},{left}Required{right},ShowInDiscoveryDocument) values (@ApiResourceId,@Description,@DisplayName,@Emphasize,@Name,@Required,@ShowInDiscoveryDocument);{_options.GetLastInsertID}", new
                {
                    ApiResourceId = apiResourceId,
                    item.Description,
                    item.DisplayName,
                    item.Emphasize,
                    item.Name,
                    item.Required,
                    item.ShowInDiscoveryDocument
                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);

                foreach (var claim in item.UserClaims)
                {
                    var ret = con.Execute($"insert into apiscopeclaims (ApiScopeId,{left}Type{right}) values (@ApiScopeId,@Type);", new
                    {
                        ApiScopeId = scopeid,
                        claim.Type
                    }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                    if (ret != 1)
                    {
                        throw new Exception($"execute insert error,return values is {ret}");
                    }
                }
            }
        }
        private void RemoveApiScopeByApiResourceId(int apiResourceId, IDbConnection con, IDbTransaction t)
        {
            con.Execute($"delete from ApiScopeClaims where ApiScopeId in (select id from ApiScopes where ApiResourceId=@ApiResourceId);", new
            {
                ApiResourceId = apiResourceId
            }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);

            con.Execute($"delete from ApiScopes where ApiResourceId=@ApiResourceId;", new
            {
                ApiResourceId = apiResourceId
            }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }
        private void RemoveApiScopes(IEnumerable<Entities.ApiScope> apiScopes, IDbConnection con, IDbTransaction t)
        {
            con.Execute($"delete from ApiScopeClaims where ApiScopeId in (@ApiScopeIds);", new
            {
                ApiScopeIds = apiScopes.Select(c => c.Id)
            }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);

            con.Execute($"delete from ApiScopes where id in (@ApiResourceIds);", new
            {
                ApiResourceIds = apiScopes.Select(c => c.Id)
            }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }

        private void UpdateScopesByApiResourceId(IEnumerable<Entities.ApiScope> apiScopes, int apiId, IDbConnection con, IDbTransaction t)
        {
            if (apiScopes.IsEmpty())
            {
                RemoveApiScopeByApiResourceId(apiId, con, t);
            }

            var dbitems = GetScopesByApiResourceId(apiId, con, t);
            if (dbitems.IsEmpty())
            {
                InsertApiScopeByApiResourceId(apiScopes, apiId, con, t);
            }

            //find deleted
            var deleteds = dbitems.Where(c => !apiScopes.ToList().Exists(d => d.Name == c.Name));
            RemoveApiScopes(deleteds, con, t);
            //find new 
            var addeds = apiScopes?.Where(c => !dbitems.ToList().Exists(d => d.Name == c.Name));
            InsertApiScopeByApiResourceId(addeds, apiId, con, t);
            //find updated
            var updateds = dbitems.Where(c => apiScopes.ToList().Exists(d => d.Name == c.Name));
            if (updateds.IsEmpty())
            {
                return;
            }

            foreach (var dbitem in updateds)
            {
                var newitem = apiScopes.FirstOrDefault(c => c.Name == dbitem.Name);
                newitem.Id = dbitem.Id;
                //update detail
                con.Execute($"update ApiScopes set {left}Description{right}=@Description,DisplayName=@DisplayName,Emphasize=@Emphasize,{left}Name{right}=@Name,{left}Required{right}=@Required,ShowInDiscoveryDocument=@ShowInDiscoveryDocument where id=@id;", newitem, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);

                if (!dbitem.UserClaims.IsEmpty())
                {
                    foreach (var claim in dbitem.UserClaims)
                    {
                        var newclaim = newitem.UserClaims.FirstOrDefault(c => c.Type == claim.Type);
                        if (newclaim == null)
                        {
                            //remove deleted
                            con.Execute($"delete from apiscopeclaims where ApiScopeId=@ApiScopeId and {left}Type{right}=@Type;", new
                            {
                                ApiScopeId = dbitem.Id,
                                Type = claim
                            }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                        }
                        else
                        {
                            //update type is the key, noting to do for update
                        }
                    }
                }

                //insert new 
                if (!newitem.UserClaims.IsEmpty())
                {
                    foreach (var newclaim in newitem.UserClaims.Where(c => !dbitem.UserClaims.Exists(d => d.Type == c.Type)))
                    {
                        con.Execute($"insert into apiscopeclaims (ApiScopeId,{left}Type{right}) values (@ApiScopeId,@Type);", new
                        {
                            ApiScopeId = dbitem.Id,
                            Type = newclaim
                        }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                    }
                }


            }

        }
        #endregion

        #region Claim
        public IEnumerable<Entities.ApiResourceClaim> GetClaimsByAPIID(int apiresourceid)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return GetClaimsByAPIID(apiresourceid, con, null);
            }
        }

        public IEnumerable<Entities.ApiResourceClaim> GetClaimsByAPIID(int apiresourceid, IDbConnection con, IDbTransaction t)
        {
            return con.Query<Entities.ApiResourceClaim>("select * from ApiClaims where ApiResourceId = @ApiResourceId", new { ApiResourceId = apiresourceid }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }

        private void InsertApiResourceClaim(Entities.ApiResourceClaim item, int apiresourceid, IDbConnection con, IDbTransaction t)
        {
            var ret = con.Execute($"insert into ApiClaims (ApiResourceId,{left}Type{right}) values (@ApiResourceId,@Type)", new
            {
                ApiResourceId = apiresourceid,
                item.Type
            }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
            if (ret != 1)
            {
                throw new Exception($"execute insert error,return values is {ret}");
            }
        }

        public void UpdateClaimsByApiResourceId(IEnumerable<Entities.ApiResourceClaim> apiResourceClaims, int apiresourceid, IDbConnection con, IDbTransaction t)
        {
            var dbitems = GetClaimsByAPIID(apiresourceid, con, t);
            if (dbitems != null)
            {
                foreach (var dbitem in dbitems)
                {
                    var newclaim = apiResourceClaims?.FirstOrDefault(c => c.Type == dbitem.Type);
                    if (newclaim == null)
                    {
                        con.Execute($"delete from ApiClaims where ApiResourceId=@ApiResourceId and {left}Type{right}=@Type;", new
                        {
                            ApiResourceId = apiresourceid,
                            dbitem.Type
                        }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                    }
                }
            }
            if (apiResourceClaims.IsEmpty())
            {
                return;
            }
            foreach (var item in apiResourceClaims.Where(c => !dbitems.ToList().Exists(d => d.Type == c.Type)))
            {
                InsertApiResourceClaim(item, apiresourceid, con, t);
            }
        }

        public void UpdateClaimsByApiResourceId(ApiResource apiResource)
        {
            var dbitem = GetByName(apiResource.Name);
            if (dbitem == null)
            {
                throw new InvalidOperationException($"could not update ApiResource {apiResource.Name} not existed");
            }
            var entity = apiResource.ToEntity();
            entity.Id = dbitem.Id;
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                var t = con.BeginTransaction();

                try
                {
                    UpdateClaimsByApiResourceId(entity.UserClaims, dbitem.Id, con, t);
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw ex;
                }
            }
        }
        #endregion

        #region ApiSecrets
        public IEnumerable<Entities.ApiSecret> GetSecretByApiResourceId(int apiresourceid)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;

                return GetSecretByApiResourceId(apiresourceid, con, null);
            }
        }

        private IEnumerable<Entities.ApiSecret> GetSecretByApiResourceId(int apiresourceid, IDbConnection con, IDbTransaction t)
        {
            return con.Query<Entities.ApiSecret>("select * from apisecrets where ApiResourceId = @ApiResourceId", new { ApiResourceId = apiresourceid }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }


        private void InsertApiSecretsByApiResourceId(Entities.ApiSecret item, int apiresourceid, IDbConnection con, IDbTransaction t)
        {
            var ret = con.Execute($"insert into ApiSecrets (ApiResourceId,{left}Description{right},Expiration,{left}Type{right},{left}Value{right}) values (@ApiResourceId,@Description,@Expiration,@Type,@Value)", new
            {
                ApiResourceId = apiresourceid,
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


        public void UpdateApiSecretsByApiResourceId(IEnumerable<Entities.ApiSecret> apiSecrets, int apiresourceid, IDbConnection con, IDbTransaction t)
        {
            var dbitems = GetSecretByApiResourceId(apiresourceid);
            if (dbitems != null)
            {
                foreach (var dbitem in dbitems)
                {
                    var newclaim = apiSecrets?.FirstOrDefault(c => c.Type == dbitem.Type && c.Value == dbitem.Value);
                    if (newclaim == null)
                    {
                        con.Execute($"delete from ApiSecrets where ApiResourceId=@ApiResourceId and {left}Type{right}=@Type and {left}Value{right}=@Value;", new
                        {
                            ApiResourceId = apiresourceid,
                            dbitem.Type,
                            Value = dbitem.Value
                        }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                    }
                    else
                    {
                        //update
                        con.Execute($"update ApiSecrets set Description=@Description,Expiration=@Expiration,{left}Type{right}=@Type,{left}Value{right}=@Value where Id=@Id;", new
                        {
                            dbitem.Id,
                            ApiResourceId = apiresourceid,
                            Description = newclaim.Description,
                            newclaim.Expiration,
                            newclaim.Type,
                            Value = newclaim.Value
                        }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                    }
                }
            }
            if (!apiSecrets.IsEmpty())
            {
                foreach (var item in apiSecrets.Where(c => !dbitems.ToList().Exists(d => d.Type == c.Type && d.Value == c.Value)))
                {
                    InsertApiSecretsByApiResourceId(item, apiresourceid, con, t);
                }
            }
        }

        public void UpdateApiSecretsByApiResourceId(ApiResource apiResource)
        {
            var entity = apiResource.ToEntity();
            var dbitem = GetByName(apiResource.Name);
            if (dbitem == null)
            {
                throw new InvalidOperationException($"could not update ApiSecrets for {apiResource.Name} which not exists.");
            }
            entity.Id = dbitem.Id;
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                UpdateApiSecretsByApiResourceId(entity.Secrets, entity.Id, null, null);
            }
        }
        #endregion

        #endregion
    }
}
