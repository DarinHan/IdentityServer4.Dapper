using Dapper;
using IdentityServer4.Dapper.Interfaces;
using IdentityServer4.Dapper.Mappers;
using IdentityServer4.Dapper.Options;
using IdentityServer4.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.Extensions.Caching.Memory;

namespace IdentityServer4.Dapper.DefaultProviders
{
    /// <summary>
    /// default client provider, which provide the query method and add or update
    /// </summary>
    public class DefaultClientProvider : IClientProvider
    {
        /// <summary>
        /// dbconfig options should be configed in each instance of db
        /// </summary>
        private DBProviderOptions _options;
        private readonly ILogger<DefaultClientProvider> _logger;
        private string left;
        private string right;

        private readonly IMemoryCache _memoryCache;

        private static volatile object locker = new object();


        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="dBProviderOptions">db config options</param>
        /// <param name="logger">the logger</param>
        public DefaultClientProvider(DBProviderOptions dBProviderOptions, ILogger<DefaultClientProvider> logger, IMemoryCache memoryCache)
        {
            this._options = dBProviderOptions ?? throw new ArgumentNullException(nameof(dBProviderOptions));
            this._logger = logger;
            this._memoryCache = memoryCache;
            left = _options.ColumnProtect["left"];
            right = _options.ColumnProtect["right"];
        }



        /// <summary>
        /// add the client to db.
        /// <para>clientid will be checked as unique key.</para> 
        /// </summary>
        /// <param name="client"></param>
        public void Add(Client client)
        {
            var dbclient = FindClientById(client.ClientId);
            if (dbclient != null)
            {
                throw new InvalidOperationException($"you can not add an existed client,clientid={client.ClientId}.");
            }
            var entity = client.ToEntity();
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                using (var t = con.BeginTransaction())
                {
                    try
                    {
                        var ClientId = con.ExecuteScalar<int>($"insert into Clients (AbsoluteRefreshTokenLifetime,AccessTokenLifetime,AccessTokenType,AllowAccessTokensViaBrowser,AllowOfflineAccess,AllowPlainTextPkce,AllowRememberConsent,AlwaysIncludeUserClaimsInIdToken,AlwaysSendClientClaims,AuthorizationCodeLifetime,BackChannelLogoutSessionRequired,BackChannelLogoutUri,ClientClaimsPrefix,ClientId,ClientName,ClientUri,ConsentLifetime,Description,EnableLocalLogin,Enabled,FrontChannelLogoutSessionRequired,FrontChannelLogoutUri,IdentityTokenLifetime,IncludeJwtId,LogoUri,PairWiseSubjectSalt,ProtocolType,RefreshTokenExpiration,RefreshTokenUsage,RequireClientSecret,RequireConsent,RequirePkce,SlidingRefreshTokenLifetime,UpdateAccessTokenClaimsOnRefresh) values (@AbsoluteRefreshTokenLifetime,@AccessTokenLifetime,@AccessTokenType,@AllowAccessTokensViaBrowser,@AllowOfflineAccess,@AllowPlainTextPkce,@AllowRememberConsent,@AlwaysIncludeUserClaimsInIdToken,@AlwaysSendClientClaims,@AuthorizationCodeLifetime,@BackChannelLogoutSessionRequired,@BackChannelLogoutUri,@ClientClaimsPrefix,@ClientId,@ClientName,@ClientUri,@ConsentLifetime,@Description,@EnableLocalLogin,@Enabled,@FrontChannelLogoutSessionRequired,@FrontChannelLogoutUri,@IdentityTokenLifetime,@IncludeJwtId,@LogoUri,@PairWiseSubjectSalt,@ProtocolType,@RefreshTokenExpiration,@RefreshTokenUsage,@RequireClientSecret,@RequireConsent,@RequirePkce,@SlidingRefreshTokenLifetime,@UpdateAccessTokenClaimsOnRefresh);{_options.GetLastInsertID}", entity, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                        var ret = 0;
                        if (entity.AllowedGrantTypes != null)
                        {
                            foreach (var item in entity.AllowedGrantTypes)
                            {
                                ret = con.Execute("insert into ClientGrantTypes (ClientId,GrantType) values (@ClientId,@GrantType)", new
                                {
                                    ClientId,
                                    item.GrantType
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }

                        if (entity.RedirectUris != null)
                        {
                            foreach (var item in entity.RedirectUris)
                            {
                                ret = con.Execute("insert into ClientRedirectUris (ClientId,RedirectUri) values (@ClientId,@RedirectUri)", new
                                {
                                    ClientId,
                                    item.RedirectUri
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        if (entity.PostLogoutRedirectUris != null)
                        {
                            foreach (var item in entity.PostLogoutRedirectUris)
                            {
                                ret = con.Execute("insert into ClientPostLogoutRedirectUris (ClientId,PostLogoutRedirectUri) values (@ClientId,@PostLogoutRedirectUri)", new
                                {
                                    ClientId,
                                    item.PostLogoutRedirectUri
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        if (entity.AllowedScopes != null)
                        {
                            foreach (var item in entity.AllowedScopes)
                            {
                                ret = con.Execute("insert into ClientScopes (ClientId,Scope) values (@ClientId,@Scope)", new
                                {
                                    ClientId,
                                    item.Scope
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        if (entity.ClientSecrets != null)
                        {
                            foreach (var item in entity.ClientSecrets)
                            {
                                ret = con.Execute("insert into ClientSecrets (ClientId,Description,Expiration,Type,Value) values (@ClientId,@Description,@Expiration,@Type,@Value)", new
                                {
                                    ClientId,
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
                        if (entity.Claims != null)
                        {
                            foreach (var item in entity.Claims)
                            {
                                ret = con.Execute("insert into ClientClaims (ClientId,Type,Value) values (@ClientId,@Type,@Value)", new
                                {
                                    ClientId,
                                    item.Type,
                                    item.Value
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        if (entity.IdentityProviderRestrictions != null)
                        {
                            foreach (var item in entity.IdentityProviderRestrictions)
                            {
                                ret = con.Execute("insert into ClientIdPRestrictions (ClientId,Provider) values (@ClientId,@Provider)", new
                                {
                                    ClientId,
                                    item.Provider,
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        if (entity.AllowedCorsOrigins != null)
                        {
                            foreach (var item in entity.AllowedCorsOrigins)
                            {
                                ret = con.Execute("insert into ClientCorsOrigins (ClientId,Origin) values (@ClientId,@Origin)", new
                                {
                                    ClientId,
                                    item.Origin,
                                }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                                if (ret != 1)
                                {
                                    throw new Exception($"execute insert error,return values is {ret}");
                                }
                            }
                        }
                        if (entity.Properties != null)
                        {
                            string left = _options.ColumnProtect["left"];
                            string right = _options.ColumnProtect["right"];
                            foreach (var item in entity.Properties)
                            {
                                ret = con.Execute($"insert into ClientProperties (ClientId,{left}Key{right},{left}Value{right}) values (@ClientId,@Key,@Value)", new
                                {
                                    ClientId,
                                    item.Key,
                                    item.Value
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

        public void Remove(string clientid)
        {
            var cliententity = GetById(clientid);
            if (cliententity == null)
            {
                return;
            }
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                using (var t = con.BeginTransaction())
                {
                    try
                    {
                        var ret = con.Execute($"delete from Clients where id=@id", new { cliententity.Id }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                        ret = con.Execute("delete from ClientGrantTypes where ClientId=@ClientId;" +
                            "delete from ClientRedirectUris where ClientId=@ClientId;" +
                            "delete from ClientPostLogoutRedirectUris where ClientId=@ClientId;" +
                            "delete from ClientScopes where ClientId=@ClientId;" +
                            "delete from ClientSecrets where ClientId=@ClientId;" +
                            "delete from ClientClaims where ClientId=@ClientId;" +
                            "delete from ClientIdPRestrictions where ClientId=@ClientId;" +
                            "delete from ClientCorsOrigins where ClientId=@ClientId;" +
                            "delete from ClientProperties where ClientId=@ClientId;", new
                            {
                                ClientId = cliententity.Id
                            }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
            }

            var key = "clients." + clientid;
            _memoryCache.Remove(key);

        }

        public void Update(Client client)
        {
            var key = "clients." + client.ClientId;

            var dbclient = FindClientById(client.ClientId);
            if (dbclient == null)
            {
                throw new InvalidOperationException($"you can not update an unexisted client,clientid={client.ClientId}.");
            }
            var entity = client.ToEntity();
            entity.Id = GetById(client.ClientId).Id;
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                using (var t = con.BeginTransaction())
                {
                    try
                    {
                        var ret = con.Execute($"update Clients set AbsoluteRefreshTokenLifetime = @AbsoluteRefreshTokenLifetime," +
                            $"AccessTokenLifetime=@AccessTokenLifetime," +
                            $"AccessTokenType=@AccessTokenType," +
                            $"AllowAccessTokensViaBrowser=@AllowAccessTokensViaBrowser," +
                            $"AllowOfflineAccess=@AllowOfflineAccess," +
                            $"AllowPlainTextPkce=@AllowPlainTextPkce," +
                            $"AllowRememberConsent=@AllowRememberConsent," +
                            $"AlwaysIncludeUserClaimsInIdToken=@AlwaysIncludeUserClaimsInIdToken," +
                            $"AlwaysSendClientClaims=@AlwaysSendClientClaims," +
                            $"AuthorizationCodeLifetime=@AuthorizationCodeLifetime," +
                            $"BackChannelLogoutSessionRequired=@BackChannelLogoutSessionRequired," +
                            $"BackChannelLogoutUri=@BackChannelLogoutUri," +
                            $"ClientClaimsPrefix=@ClientClaimsPrefix," +
                            $"ClientName=@ClientName," +
                            $"ClientUri=@ClientUri," +
                            $"ConsentLifetime=@ConsentLifetime," +
                            $"Description=@Description," +
                            $"EnableLocalLogin=@EnableLocalLogin," +
                            $"Enabled=@Enabled," +
                            $"FrontChannelLogoutSessionRequired=@FrontChannelLogoutSessionRequired," +
                            $"FrontChannelLogoutUri=@FrontChannelLogoutUri," +
                            $"IdentityTokenLifetime=@IdentityTokenLifetime," +
                            $"IncludeJwtId=@IncludeJwtId," +
                            $"LogoUri=@LogoUri," +
                            $"PairWiseSubjectSalt=@PairWiseSubjectSalt," +
                            $"ProtocolType=@ProtocolType," +
                            $"RefreshTokenExpiration=@RefreshTokenExpiration," +
                            $"RefreshTokenUsage=@RefreshTokenUsage," +
                            $"RequireClientSecret=@RequireClientSecret," +
                            $"RequireConsent=@RequireConsent," +
                            $"RequirePkce=@RequirePkce," +
                            $"SlidingRefreshTokenLifetime=@SlidingRefreshTokenLifetime," +
                            $"UpdateAccessTokenClaimsOnRefresh=@UpdateAccessTokenClaimsOnRefresh where ClientId=@ClientId;", entity, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);

                        UpdateClientGrantTypeByClientID(entity.AllowedGrantTypes, entity.Id, con, t);
                        UpdateClientRedirectUriByClientID(entity.RedirectUris, entity.Id, con, t);
                        UpdateClientPostLogoutRedirectUriByClientID(entity.PostLogoutRedirectUris, entity.Id, con, t);
                        UpdateClientScopeByClientID(entity.AllowedScopes, entity.Id, con, t);
                        UpdateClientSecretByClientID(entity.ClientSecrets, entity.Id, con, t);
                        UpdateClientClaimByClientID(entity.Claims, entity.Id, con, t);
                        UpdateClientIdPRestrictionByClientID(entity.IdentityProviderRestrictions, entity.Id, con, t);
                        UpdateClientCorsOriginByClientID(entity.AllowedCorsOrigins, entity.Id, con, t);
                        UpdateClientPropertyByClientID(entity.Properties, entity.Id, con, t);
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
            }

            _memoryCache.Remove(key);
        }


        #region Query
        /// <summary>
        /// find client by client id.
        /// <para>make this method virtual for override in subclass.</para>
        /// </summary>
        /// <param name="clientid"></param>
        /// <returns></returns>
        public virtual Client FindClientById(string clientid)
        {
            var key = "clients." + clientid;
            var clientmodel = _memoryCache.Get<Client>(key);
            if (clientmodel == null)
            {
                lock (locker)
                {
                    clientmodel = _memoryCache.Get<Client>(key);
                    if (clientmodel != null)
                    {
                        return clientmodel;
                    }

                    var client = GetById(clientid);
                    if (client == null)
                    {
                        return null;
                    }

                    using (var connection = _options.DbProviderFactory.CreateConnection())
                    {
                        connection.ConnectionString = _options.ConnectionString;

                        if (client != null)
                        {
                            //do not use the mutiquery in case of some db can not return muti sets
                            //if you want to redurce the time cost,please recode in your own class which should inherit from IClientProvider or this
                            var granttypes = GetClientGrantTypeByClientID(client.Id);
                            var redirecturls = GetClientRedirectUriByClientID(client.Id);
                            var postlogoutredirecturis = GetClientPostLogoutRedirectUriByClientID(client.Id);
                            var allowedscopes = GetClientScopeByClientID(client.Id);
                            var secrets = GetClientSecretByClientID(client.Id);
                            var claims = GetClientClaimByClientID(client.Id);
                            var iprestrictions = GetClientIdPRestrictionByClientID(client.Id);
                            var corsOrigins = GetClientCorsOriginByClientID(client.Id);
                            var properties = GetClientPropertyByClientID(client.Id);

                            if (granttypes != null)
                            {
                                foreach (var item in granttypes)
                                {
                                    item.Client = client;
                                }
                                client.AllowedGrantTypes = granttypes.AsList();
                            }
                            if (redirecturls != null)
                            {
                                foreach (var item in redirecturls)
                                {
                                    item.Client = client;
                                }
                                client.RedirectUris = redirecturls.AsList();
                            }

                            if (postlogoutredirecturis != null)
                            {
                                foreach (var item in postlogoutredirecturis)
                                {
                                    item.Client = client;
                                }
                                client.PostLogoutRedirectUris = postlogoutredirecturis.AsList();
                            }
                            if (allowedscopes != null)
                            {
                                foreach (var item in allowedscopes)
                                {
                                    item.Client = client;
                                }
                                client.AllowedScopes = allowedscopes.AsList();
                            }
                            if (secrets != null)
                            {
                                foreach (var item in secrets)
                                {
                                    item.Client = client;
                                }
                                client.ClientSecrets = secrets.AsList();
                            }
                            if (claims != null)
                            {
                                foreach (var item in claims)
                                {
                                    item.Client = client;
                                }
                                client.Claims = claims.AsList();
                            }
                            if (iprestrictions != null)
                            {
                                foreach (var item in iprestrictions)
                                {
                                    item.Client = client;
                                }
                                client.IdentityProviderRestrictions = iprestrictions.AsList();
                            }
                            if (corsOrigins != null)
                            {
                                foreach (var item in corsOrigins)
                                {
                                    item.Client = client;
                                }
                                client.AllowedCorsOrigins = corsOrigins.AsList();
                            }

                            if (properties != null)
                            {
                                foreach (var item in properties)
                                {
                                    item.Client = client;
                                }
                                client.Properties = properties.AsList();
                            }
                        }

                        clientmodel = client?.ToModel();
                    }

                    _memoryCache.Set<Client>(key, clientmodel, TimeSpan.FromHours(24));
                }
            }

            return clientmodel;
        }

        public bool Exist(string clientid)
        {
            var client = FindClientById(clientid);
            return client != null;
        }

        public Entities.Client GetById(string clientid)
        {
            if (string.IsNullOrWhiteSpace(clientid))
            {
                return null;
            }

            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;

                return connection.QueryFirstOrDefault<Entities.Client>("select * from Clients where ClientId = @ClientId", new { ClientId = clientid }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);

            }
        }
        public IEnumerable<string> QueryAllowedCorsOrigins()
        {
            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;
                var corsOrigins = connection.Query<string>("select distinct Origin from ClientCorsOrigins where Origin is not null", commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                return corsOrigins;
            }
        }

        public IEnumerable<Client> Search(string keywords, int pageIndex, int pageSize, out int totalCount)
        {
            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;

                DynamicParameters pairs = new DynamicParameters();
                pairs.Add("keywords", "%" + keywords + "%");

                var countsql = "select count(1) from Clients where ClientId like @keywords or ClientName like @keywords";
                totalCount = connection.ExecuteScalar<int>(countsql, pairs, commandType: CommandType.Text);

                if (totalCount == 0)
                {
                    return null;
                }

                var clients = connection.Query<Entities.Client>(_options.GetPageQuerySQL("select * from Clients where ClientId like @keywords or ClientName like @keywords", pageIndex, pageSize, totalCount, "", pairs), pairs, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                if (clients != null)
                {
                    return clients.Select(c => c.ToModel());
                }
                return null;
            }
        }
        #endregion


        #region 子属性
        #region ClientGrantType
        public IEnumerable<Entities.ClientGrantType> GetClientGrantTypeByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return GetClientGrantTypeByClientID(ClientId, con, null);
            }
        }

        private IEnumerable<Entities.ClientGrantType> GetClientGrantTypeByClientID(int ClientId, IDbConnection con, IDbTransaction t)
        {
            return con.Query<Entities.ClientGrantType>("select * from ClientGrantTypes where  ClientId = @ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }

        public void UpdateClientGrantTypeByClientID(IEnumerable<Entities.ClientGrantType> clientGrantTypes, int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                var t = con.BeginTransaction();

                try
                {
                    UpdateClientGrantTypeByClientID(clientGrantTypes, ClientId, con, t);
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw ex;
                }
            }
        }

        private void UpdateClientGrantTypeByClientID(IEnumerable<Entities.ClientGrantType> clientGrantTypes, int ClientId, IDbConnection con, IDbTransaction t)
        {
            var dbitems = GetClientGrantTypeByClientID(ClientId, con, t);
            List<Entities.ClientGrantType> joined = null;
            List<Entities.ClientGrantType> added = null;
            if (dbitems != null && dbitems.Count() > 0)
            {
                joined = (from a in dbitems
                          join b in clientGrantTypes
                          on a.GrantType equals b.GrantType
                          select a).ToList();
            }
            if (joined.IsEmpty())
            {
                added = clientGrantTypes.ToList();
            }
            else
            {
                added = clientGrantTypes.Where(c => !joined.Exists(d => d.GrantType == c.GrantType)).ToList();
            }

            if (!dbitems.IsEmpty())
            {
                var grants = dbitems.Where(c => !joined.Exists(d => d.GrantType == c.GrantType)).Select(c => c.GrantType).ToArray();
                if (!grants.IsEmpty())
                {
                    con.Execute("delete from ClientGrantTypes where  ClientId = @ClientId and GrantType in @GrantTypes", new { ClientId = ClientId, GrantTypes = grants }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                }
            }

            if (!added.IsEmpty())
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                System.Text.StringBuilder sql = new System.Text.StringBuilder();
                int index = 0;
                foreach (var item in added)
                {
                    sql.Append($"insert into ClientGrantTypes (ClientId,GrantType) values (@ClientId{index},@GrantType{index});");
                    dynamicParameters.Add($"ClientId{index}", ClientId);
                    dynamicParameters.Add($"GrantType{index}", item.GrantType);
                    index++;
                }
                con.Execute(sql.ToString(), dynamicParameters, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
            }

        }
        #endregion

        #region ClientRedirectUri
        public IEnumerable<Entities.ClientRedirectUri> GetClientRedirectUriByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return GetClientRedirectUriByClientID(ClientId, con, null);
            }
        }

        public IEnumerable<Entities.ClientRedirectUri> GetClientRedirectUriByClientID(int ClientId, IDbConnection con, IDbTransaction t)
        {
            return con.Query<Entities.ClientRedirectUri>("select * from ClientRedirectUris where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }
        public void UpdateClientRedirectUriByClientID(IEnumerable<Entities.ClientRedirectUri> clientRedirectUris, int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                var t = con.BeginTransaction();

                try
                {
                    UpdateClientRedirectUriByClientID(clientRedirectUris, ClientId, con, t);
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw ex;
                }
            }
        }

        private void UpdateClientRedirectUriByClientID(IEnumerable<Entities.ClientRedirectUri> clientRedirectUris, int ClientId, IDbConnection con, IDbTransaction t)
        {
            var dbitems = GetClientRedirectUriByClientID(ClientId, con, t);
            List<Entities.ClientRedirectUri> joined = null;
            List<Entities.ClientRedirectUri> added = null;
            if (dbitems != null && dbitems.Count() > 0)
            {
                joined = (from a in dbitems
                          join b in clientRedirectUris
                          on a.RedirectUri equals b.RedirectUri
                          select a).ToList();
            }
            if (joined.IsEmpty())
            {
                added = clientRedirectUris.ToList();
            }
            else
            {
                added = clientRedirectUris.Where(c => !joined.Exists(d => d.RedirectUri == c.RedirectUri)).ToList();
            }

            if (!dbitems.IsEmpty())
            {
                var grants = dbitems.Where(c => !joined.Exists(d => d.RedirectUri == c.RedirectUri)).Select(c => c.RedirectUri).ToArray();
                if (!grants.IsEmpty())
                {
                    con.Execute("delete from ClientRedirectUris where  ClientId = @ClientId and RedirectUri in @RedirectUris", new { ClientId = ClientId, RedirectUris = grants }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                }
            }

            if (!added.IsEmpty())
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                System.Text.StringBuilder sql = new System.Text.StringBuilder();
                int index = 0;
                foreach (var item in added)
                {
                    sql.Append($"insert into ClientRedirectUris (ClientId,RedirectUri) values (@ClientId{index},@RedirectUri{index});");
                    dynamicParameters.Add($"ClientId{index}", ClientId);
                    dynamicParameters.Add($"RedirectUri{index}", item.RedirectUri);
                    index++;
                }

                con.Execute(sql.ToString(), dynamicParameters, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
            }

        }
        #endregion

        #region ClientPostLogoutRedirectUri
        public IEnumerable<Entities.ClientPostLogoutRedirectUri> GetClientPostLogoutRedirectUriByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return GetClientPostLogoutRedirectUriByClientID(ClientId, con, null);
            }
        }
        public IEnumerable<Entities.ClientPostLogoutRedirectUri> GetClientPostLogoutRedirectUriByClientID(int ClientId, IDbConnection con, IDbTransaction t)
        {
            return con.Query<Entities.ClientPostLogoutRedirectUri>("select * from ClientPostLogoutRedirectUris where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }
        public void UpdateClientPostLogoutRedirectUriByClientID(IEnumerable<Entities.ClientPostLogoutRedirectUri> clientPostLogoutRedirectUris, int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                var t = con.BeginTransaction();

                try
                {
                    UpdateClientPostLogoutRedirectUriByClientID(clientPostLogoutRedirectUris, ClientId, con, t);
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw ex;
                }
            }
        }

        private void UpdateClientPostLogoutRedirectUriByClientID(IEnumerable<Entities.ClientPostLogoutRedirectUri> clientPostLogoutRedirectUris, int ClientId, IDbConnection con, IDbTransaction t)
        {
            var dbitems = GetClientPostLogoutRedirectUriByClientID(ClientId, con, t);
            List<Entities.ClientPostLogoutRedirectUri> joined = null;
            List<Entities.ClientPostLogoutRedirectUri> added = null;
            if (dbitems != null && dbitems.Count() > 0)
            {
                joined = (from a in dbitems
                          join b in clientPostLogoutRedirectUris
                          on a.PostLogoutRedirectUri equals b.PostLogoutRedirectUri
                          select a).ToList();
            }
            if (joined.IsEmpty())
            {
                added = clientPostLogoutRedirectUris.ToList();
            }
            else
            {
                added = clientPostLogoutRedirectUris.Where(c => !joined.Exists(d => d.PostLogoutRedirectUri == c.PostLogoutRedirectUri)).ToList();
            }

            if (!dbitems.IsEmpty())
            {
                var grants = dbitems.Where(c => !joined.Exists(d => d.PostLogoutRedirectUri == c.PostLogoutRedirectUri)).Select(c => c.PostLogoutRedirectUri).ToArray();
                if (!grants.IsEmpty())
                {
                    con.Execute("delete from ClientPostLogoutRedirectUris where  ClientId = @ClientId and PostLogoutRedirectUri in @PostLogoutRedirectUris", new { ClientId = ClientId, PostLogoutRedirectUris = grants }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                }
            }

            if (!added.IsEmpty())
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                System.Text.StringBuilder sql = new System.Text.StringBuilder();
                int index = 0;
                foreach (var item in added)
                {
                    sql.Append($"insert into ClientPostLogoutRedirectUris (ClientId,PostLogoutRedirectUri) values (@ClientId{index},@PostLogoutRedirectUri{index});");
                    dynamicParameters.Add($"ClientId{index}", ClientId);
                    dynamicParameters.Add($"PostLogoutRedirectUri{index}", item.PostLogoutRedirectUri);
                    index++;
                }

                con.Execute(sql.ToString(), dynamicParameters, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
            }
        }
        #endregion

        #region ClientScope
        public IEnumerable<Entities.ClientScope> GetClientScopeByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return GetClientScopeByClientID(ClientId, con, null);
            }
        }
        public IEnumerable<Entities.ClientScope> GetClientScopeByClientID(int ClientId, IDbConnection con, IDbTransaction t)
        {
            return con.Query<Entities.ClientScope>("select * from ClientScopes where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }
        public void UpdateClientScopeByClientID(IEnumerable<Entities.ClientScope> clientScopes, int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                var t = con.BeginTransaction();

                try
                {
                    UpdateClientScopeByClientID(clientScopes, ClientId, con, t);
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw ex;
                }
            }
        }

        private void UpdateClientScopeByClientID(IEnumerable<Entities.ClientScope> clientScopes, int ClientId, IDbConnection con, IDbTransaction t)
        {
            var dbitems = GetClientScopeByClientID(ClientId, con, t);
            List<Entities.ClientScope> joined = null;
            List<Entities.ClientScope> added = null;
            if (dbitems != null && dbitems.Count() > 0)
            {
                joined = (from a in dbitems
                          join b in clientScopes
                          on a.Scope equals b.Scope
                          select a).ToList();
            }
            if (joined.IsEmpty())
            {
                added = clientScopes.ToList();
            }
            else
            {
                added = clientScopes.Where(c => !joined.Exists(d => d.Scope == c.Scope)).ToList();
            }

            if (!dbitems.IsEmpty())
            {
                var grants = dbitems.Where(c => !joined.Exists(d => d.Scope == c.Scope)).Select(c => c.Scope).ToArray();
                if (!grants.IsEmpty())
                {
                    con.Execute("delete from ClientScopes where  ClientId = @ClientId and Scope in @Scopes;", new { ClientId = ClientId, Scopes = grants }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                }
            }

            if (!added.IsEmpty())
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                System.Text.StringBuilder sql = new System.Text.StringBuilder();
                int index = 0;
                foreach (var item in added)
                {
                    sql.Append($"insert into ClientScopes (ClientId,Scope) values (@ClientId{index},@Scope{index});");
                    dynamicParameters.Add($"ClientId{index}", ClientId);
                    dynamicParameters.Add($"Scope{index}", item.Scope);
                    index++;
                }

                con.Execute(sql.ToString(), dynamicParameters, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
            }
        }
        #endregion

        #region ClientSecretByClientID
        public IEnumerable<Entities.ClientSecret> GetClientSecretByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return GetClientSecretByClientID(ClientId, con, null);
            }
        }
        public IEnumerable<Entities.ClientSecret> GetClientSecretByClientID(int ClientId, IDbConnection con, IDbTransaction t)
        {
            return con.Query<Entities.ClientSecret>("select * from ClientSecrets where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }
        public void UpdateClientSecretByClientID(IEnumerable<Entities.ClientSecret> clientSecrets, int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                var t = con.BeginTransaction();

                try
                {
                    UpdateClientSecretByClientID(clientSecrets, ClientId, con, t);
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw ex;
                }
            }
        }

        private void UpdateClientSecretByClientID(IEnumerable<Entities.ClientSecret> clientSecrets, int ClientId, IDbConnection con, IDbTransaction t)
        {
            var dbitems = GetClientSecretByClientID(ClientId, con, t);
            List<Entities.ClientSecret> joined = null;
            List<Entities.ClientSecret> added = null;
            if (dbitems != null && dbitems.Count() > 0)
            {
                joined = (from a in dbitems
                          join b in clientSecrets
                          on new { a.Value, a.Type } equals new { b.Value, b.Type }
                          select a).ToList();
            }
            if (joined.IsEmpty())
            {
                added = clientSecrets.ToList();
            }
            else
            {
                added = clientSecrets.Where(c => !joined.Exists(d => d.Value == c.Value && d.Type == c.Type)).ToList();
            }

            if (!dbitems.IsEmpty())
            {
                foreach (var item in dbitems)
                {
                    if (!joined.Exists(c => c.Type == item.Type && c.Value == item.Value))
                    {
                        con.Execute($"delete from ClientSecrets where ClientId = @ClientId and {left}Value{right} = @Value and {left}Type{right}=@Type;", new { ClientId = ClientId, Value = item.Value, item.Type }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                    }
                }
            }

            if (!added.IsEmpty())
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                System.Text.StringBuilder sql = new System.Text.StringBuilder();
                int index = 0;
                foreach (var item in added)
                {
                    sql.Append($"insert into ClientSecrets (ClientId,Description,Expiration,Type,Value) values (@ClientId{index},@Description{index},@Expiration{index},@Type{index},@Value{index});");
                    dynamicParameters.Add($"ClientId{index}", ClientId);
                    dynamicParameters.Add($"Description{index}", item.Description);
                    dynamicParameters.Add($"Expiration{index}", item.Expiration);
                    dynamicParameters.Add($"Type{index}", item.Type);
                    dynamicParameters.Add($"Value{index}", item.Value);
                    index++;
                }

                con.Execute(sql.ToString(), dynamicParameters, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
            }
        }

        #endregion

        #region ClientClaim
        public IEnumerable<Entities.ClientClaim> GetClientClaimByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return GetClientClaimByClientID(ClientId, con, null);
            }
        }
        public IEnumerable<Entities.ClientClaim> GetClientClaimByClientID(int ClientId, IDbConnection con, IDbTransaction t)
        {
            return con.Query<Entities.ClientClaim>("select * from ClientClaims where ClientId=@ClientId", new
            {
                ClientId = ClientId
            }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }
        public void UpdateClientClaimByClientID(IEnumerable<Entities.ClientClaim> clientClaims, int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                var t = con.BeginTransaction();

                try
                {
                    UpdateClientClaimByClientID(clientClaims, ClientId, con, t);
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw ex;
                }
            }
        }

        private void UpdateClientClaimByClientID(IEnumerable<Entities.ClientClaim> clientClaims, int ClientId, IDbConnection con, IDbTransaction t)
        {
            var dbitems = GetClientClaimByClientID(ClientId, con, t);
            List<Entities.ClientClaim> joined = null;
            List<Entities.ClientClaim> added = null;
            if (dbitems != null && dbitems.Count() > 0)
            {
                joined = (from a in dbitems
                          join b in clientClaims
                          on new { a.Value, a.Type } equals new { b.Value, b.Type }
                          select a).ToList();
            }
            if (joined.IsEmpty())
            {
                added = clientClaims.ToList();
            }
            else
            {
                added = clientClaims.Where(c => !joined.Exists(d => d.Value == c.Value && d.Type == c.Type)).ToList();
            }

            if (!dbitems.IsEmpty())
            {
                foreach (var item in dbitems)
                {
                    if (!joined.Exists(c => c.Type == item.Type && c.Value == item.Value))
                    {
                        con.Execute($"delete from ClientClaims where ClientId = @ClientId and {left}Value{right} = @Value and {left}Type{right}=@Type;", new { ClientId = ClientId, Value = item.Value, item.Type }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                    }
                }
            }

            if (!added.IsEmpty())
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                System.Text.StringBuilder sql = new System.Text.StringBuilder();
                int index = 0;
                foreach (var item in added)
                {
                    sql.Append($"insert into ClientClaims (ClientId,Type,Value) values (@ClientId{index},@Type{index},@Value{index});");
                    dynamicParameters.Add($"ClientId{index}", ClientId);
                    dynamicParameters.Add($"Type{index}", item.Type);
                    dynamicParameters.Add($"Value{index}", item.Value);
                    index++;
                }

                con.Execute(sql.ToString(), dynamicParameters, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
            }
        }
        #endregion

        #region ClientIdPRestriction
        public IEnumerable<Entities.ClientIdPRestriction> GetClientIdPRestrictionByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return GetClientIdPRestrictionByClientID(ClientId, con, null);
            }
        }
        public IEnumerable<Entities.ClientIdPRestriction> GetClientIdPRestrictionByClientID(int ClientId, IDbConnection con, IDbTransaction t)
        {
            return con.Query<Entities.ClientIdPRestriction>("select * from ClientIdPRestrictions where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }

        public void UpdateClientIdPRestrictionByClientID(IEnumerable<Entities.ClientIdPRestriction> clientIdPRestrictions, int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                var t = con.BeginTransaction();

                try
                {
                    UpdateClientIdPRestrictionByClientID(clientIdPRestrictions, ClientId, con, t);
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw ex;
                }
            }
        }

        private void UpdateClientIdPRestrictionByClientID(IEnumerable<Entities.ClientIdPRestriction> clientIdPRestrictions, int ClientId, IDbConnection con, IDbTransaction t)
        {
            var dbitems = GetClientIdPRestrictionByClientID(ClientId, con, t);
            List<Entities.ClientIdPRestriction> joined = null;
            List<Entities.ClientIdPRestriction> added = null;
            if (dbitems != null && dbitems.Count() > 0)
            {
                joined = (from a in dbitems
                          join b in clientIdPRestrictions
                          on a.Provider equals b.Provider
                          select a).ToList();
            }
            if (joined.IsEmpty())
            {
                added = clientIdPRestrictions.ToList();
            }
            else
            {
                added = clientIdPRestrictions.Where(c => !joined.Exists(d => d.Provider == c.Provider)).ToList();
            }

            if (!dbitems.IsEmpty())
            {
                var grants = dbitems.Where(c => !joined.Exists(d => d.Provider == c.Provider)).Select(c => c.Provider).ToArray();
                if (!grants.IsEmpty())
                {
                    con.Execute("delete from ClientIdPRestrictions where  ClientId = @ClientId and Provider in @Providers", new { ClientId = ClientId, Providers = grants }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                }
            }

            if (!added.IsEmpty())
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                System.Text.StringBuilder sql = new System.Text.StringBuilder();
                int index = 0;
                foreach (var item in added)
                {
                    sql.Append($"insert into ClientIdPRestrictions (ClientId,Provider) values (@ClientId{index},@Provider{index});");
                    dynamicParameters.Add($"ClientId{index}", ClientId);
                    dynamicParameters.Add($"Provider{index}", item.Provider);
                    index++;
                }

                con.Execute(sql.ToString(), dynamicParameters, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
            }
        }
        #endregion

        #region ClientCorsOriginByClientID
        public IEnumerable<Entities.ClientCorsOrigin> GetClientCorsOriginByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return GetClientCorsOriginByClientID(ClientId, con, null);
            }
        }
        public IEnumerable<Entities.ClientCorsOrigin> GetClientCorsOriginByClientID(int ClientId, IDbConnection con, IDbTransaction t)
        {
            return con.Query<Entities.ClientCorsOrigin>("select * from ClientCorsOrigins where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }

        public void UpdateClientCorsOriginByClientID(IEnumerable<Entities.ClientCorsOrigin> clientCorsOrigins, int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                var t = con.BeginTransaction();

                try
                {
                    UpdateClientCorsOriginByClientID(clientCorsOrigins, ClientId, con, t);
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw ex;
                }
            }
        }

        private void UpdateClientCorsOriginByClientID(IEnumerable<Entities.ClientCorsOrigin> clientCorsOrigins, int ClientId, IDbConnection con, IDbTransaction t)
        {
            var dbitems = GetClientCorsOriginByClientID(ClientId, con, t);
            List<Entities.ClientCorsOrigin> joined = null;
            List<Entities.ClientCorsOrigin> added = null;
            if (dbitems != null && dbitems.Count() > 0)
            {
                joined = (from a in dbitems
                          join b in clientCorsOrigins
                          on a.Origin equals b.Origin
                          select a).ToList();
            }
            if (joined.IsEmpty())
            {
                added = clientCorsOrigins.ToList();
            }
            else
            {
                added = clientCorsOrigins.Where(c => !joined.Exists(d => d.Origin == c.Origin)).ToList();
            }

            if (!dbitems.IsEmpty())
            {
                var grants = dbitems.Where(c => !joined.Exists(d => d.Origin == c.Origin)).Select(c => c.Origin).ToArray();
                if (!grants.IsEmpty())
                {
                    con.Execute("delete from ClientCorsOrigins  where  ClientId = @ClientId and Origin in @Origins", new { ClientId = ClientId, Origins = grants }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                }
            }

            if (!added.IsEmpty())
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                System.Text.StringBuilder sql = new System.Text.StringBuilder();
                int index = 0;
                foreach (var item in added)
                {
                    sql.Append($"insert into ClientCorsOrigins (ClientId,Origin) values (@ClientId{index},@Origin{index});");
                    dynamicParameters.Add($"ClientId{index}", ClientId);
                    dynamicParameters.Add($"Origin{index}", item.Origin);
                    index++;
                }

                con.Execute(sql.ToString(), dynamicParameters, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
            }
        }
        #endregion

        #region ClientProperty
        public IEnumerable<Entities.ClientProperty> GetClientPropertyByClientID(int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                return GetClientPropertyByClientID(ClientId, con, null);
            }
        }
        public IEnumerable<Entities.ClientProperty> GetClientPropertyByClientID(int ClientId, IDbConnection con, IDbTransaction t)
        {
            return con.Query<Entities.ClientProperty>("select * from ClientProperties where ClientId=@ClientId", new { ClientId = ClientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
        }

        public void UpdateClientPropertyByClientID(IEnumerable<Entities.ClientProperty> clientProperties, int ClientId)
        {
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                var t = con.BeginTransaction();

                try
                {
                    UpdateClientPropertyByClientID(clientProperties, ClientId, con, t);
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    throw ex;
                }
            }
        }

        private void UpdateClientPropertyByClientID(IEnumerable<Entities.ClientProperty> clientProperties, int ClientId, IDbConnection con, IDbTransaction t)
        {
            var dbitems = GetClientPropertyByClientID(ClientId, con, t);
            List<Entities.ClientProperty> joined = null;
            List<Entities.ClientProperty> added = null;
            if (dbitems != null && dbitems.Count() > 0)
            {
                joined = (from a in dbitems
                          join b in clientProperties
                          on new { a.Key, a.Value } equals new { b.Key, b.Value }
                          select a).ToList();
            }
            if (joined.IsEmpty())
            {
                added = clientProperties.ToList();
            }
            else
            {
                added = clientProperties.Where(c => !joined.Exists(d => d.Key == c.Key && d.Value == c.Value)).ToList();
            }

            if (!dbitems.IsEmpty())
            {
                foreach (var item in dbitems)
                {
                    if (!joined.Exists(c => c.Key == item.Key && c.Value == item.Value))
                    {
                        con.Execute($"delete from ClientProperties where ClientId = @ClientId and {left}Value{right} = @Value and {left}Key{right}=@Key;", new { ClientId = ClientId, Value = item.Value, item.Key }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                    }
                }
            }

            if (!added.IsEmpty())
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                System.Text.StringBuilder sql = new System.Text.StringBuilder();
                int index = 0;
                foreach (var item in added)
                {
                    sql.Append($"insert into ClientProperties (ClientId,{left}Key{right},{left}Value{right}) values (@ClientId{index},@Key{index},@Value{index});");
                    dynamicParameters.Add($"ClientId{index}", ClientId);
                    dynamicParameters.Add($"Key{index}", item.Key);
                    dynamicParameters.Add($"Value{index}", item.Value);
                    index++;
                }

                con.Execute(sql.ToString(), dynamicParameters, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
            }
        }
        #endregion



        private Entities.Client GetClientEntity(Client client)
        {
            var dbclient = GetById(client.ClientId);
            if (dbclient == null)
            {
                throw new InvalidOperationException($"you can not update an unexisted client,clientid={client.ClientId}.");
            }
            var entity = client.ToEntity();
            entity.Id = dbclient.Id;
            return entity;
        }

        public void UpdateGrantTypes(Client client)
        {
            var entity = GetClientEntity(client);
            UpdateClientGrantTypeByClientID(entity.AllowedGrantTypes, entity.Id);
        }

        public void UpdateRedirectUris(Client client)
        {
            var entity = GetClientEntity(client);
            UpdateClientRedirectUriByClientID(entity.RedirectUris, entity.Id);
        }

        public void UpdatePostLogoutRedirectUris(Client client)
        {
            var entity = GetClientEntity(client);
            UpdateClientPostLogoutRedirectUriByClientID(entity.PostLogoutRedirectUris, entity.Id);
        }

        public void UpdateScopes(Client client)
        {
            var entity = GetClientEntity(client);
            UpdateClientScopeByClientID(entity.AllowedScopes, entity.Id);
        }

        public void UpdateSecrets(Client client)
        {
            var entity = GetClientEntity(client);
            UpdateClientSecretByClientID(entity.ClientSecrets, entity.Id);
        }

        public void UpdateClaims(Client client)
        {
            var entity = GetClientEntity(client);
            UpdateClientClaimByClientID(entity.Claims, entity.Id);
        }

        public void UpdateIdPRestrictions(Client client)
        {
            var entity = GetClientEntity(client);
            UpdateClientIdPRestrictionByClientID(entity.IdentityProviderRestrictions, entity.Id);
        }

        public void UpdateCorsOrigins(Client client)
        {
            var entity = GetClientEntity(client);
            UpdateClientCorsOriginByClientID(entity.AllowedCorsOrigins, entity.Id);
        }

        public void UpdatePropertys(Client client)
        {
            var entity = GetClientEntity(client);
            UpdateClientPropertyByClientID(entity.Properties, entity.Id);
        }



        #endregion
    }
}
