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
    public class DefaultPersistedGrantProvider : IPersistedGrantProvider, IPersistedGrantStoreClanup
    {
        private DBProviderOptions _options;
        private readonly ILogger<DefaultPersistedGrantProvider> _logger;

        public DefaultPersistedGrantProvider(DBProviderOptions dBProviderOptions, ILogger<DefaultPersistedGrantProvider> logger)
        {
            this._options = dBProviderOptions ?? throw new ArgumentNullException(nameof(dBProviderOptions));
            this._logger = logger;
        }


        public void Add(PersistedGrant token)
        {
            var entity = token.ToEntity();
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                using (var t = con.BeginTransaction())
                {
                    try
                    {
                        var ret = con.Execute("insert into PersistedGrants (Key,ClientId,CreationTime,Data,Expiration,SubjectId,Type) values (@Key,@ClientId,@CreationTime,@Data,@Expiration,@SubjectId,@Type,)", new
                        {
                            entity.Key,
                            entity.ClientId,
                            entity.CreationTime,
                            entity.Data,
                            entity.Expiration,
                            entity.SubjectId,
                            entity.Type
                        }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                        if (ret != 1)
                        {
                            throw new Exception($"execute insert error,return values is {ret}");
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

        public PersistedGrant Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }
            Entities.PersistedGrant persistedGrant = null;
            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;
                persistedGrant = connection.QueryFirstOrDefault<Entities.PersistedGrant>("select * from PersistedGrants where Key = @Key", new { Key = key }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
            }
            return persistedGrant?.ToModel();
        }

        public IEnumerable<PersistedGrant> GetAll(string subjectId)
        {
            return GetAll(subjectId, null, null);

        }

        public IEnumerable<PersistedGrant> GetAll(string subjectId, string clientId)
        {
            return GetAll(subjectId, clientId, null);
        }

        public IEnumerable<PersistedGrant> GetAll(string subjectId, string clientId, string type)
        {
            if (string.IsNullOrWhiteSpace(subjectId))
            {
                return null;
            }

            clientId = string.IsNullOrWhiteSpace(clientId) ? null : clientId;
            type = string.IsNullOrWhiteSpace(type) ? null : type;

            IEnumerable<Entities.PersistedGrant> persistedGrants = null;
            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;
                persistedGrants = connection.Query<Entities.PersistedGrant>("select * from PersistedGrants where (SubjectId = @SubjectId or @SubjectId is null) and (ClientId = @ClientId or @ClientId is null) and (Type = @Type or @Type is null)", new { SubjectId = subjectId, ClientId = clientId, Type = type }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
            }
            return persistedGrants?.Select(c => c.ToModel());
        }

        public int QueryExpired(DateTime dateTime)
        {
            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;
                var count = connection.ExecuteScalar<int>("select count(1) from PersistedGrants p where p.Expiration < @UtcNow", new { UtcNow = dateTime }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text);
                return count;
            }
        }

        public void Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }
            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;
                connection.Open();
                using (var t = connection.BeginTransaction())
                {
                    try
                    {
                        var ret = connection.Execute("delete PersistedGrants where Key = @Key", new { Key = key }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
                connection.Close();
            }
        }

        public void RemoveAll(string subjectId, string clientId)
        {
            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;
                connection.Open();
                using (var t = connection.BeginTransaction())
                {
                    try
                    {
                        var ret = connection.Execute("delete PersistedGrants where SubjectId = @SubjectId and ClientId = @ClientId", new { SubjectId = subjectId, ClientId = clientId }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
                connection.Close();
            }
        }

        public void RemoveAll(string subjectId, string clientId, string type)
        {
            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;
                connection.Open();
                using (var t = connection.BeginTransaction())
                {
                    try
                    {
                        var ret = connection.Execute("delete PersistedGrants where SubjectId = @SubjectId and ClientId = @ClientId and Type = @Type", new { SubjectId = subjectId, ClientId = clientId, Type = type }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
                connection.Close();
            }
        }

        public void RemoveRange(DateTime dateTime)
        {
            using (var connection = _options.DbProviderFactory.CreateConnection())
            {
                connection.ConnectionString = _options.ConnectionString;
                connection.Open();

                using (var t = connection.BeginTransaction())
                {
                    try
                    {
                        var ret = connection.Execute("delete PersistedGrants where Expiration < @UtcNow", new { UtcNow = dateTime }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                        t.Commit();
                    }
                    catch (Exception ex)
                    {
                        t.Rollback();
                        throw ex;
                    }
                }
                connection.Close();
            }
        }

        public void Store(PersistedGrant grant)
        {
            var dbgrant = Get(grant.Key);
            if (dbgrant != null)
            {
                throw new InvalidOperationException($"you can not add an existed PersistedGrant,key={grant.Key}.");
            }
            var entity = grant.ToEntity();
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                using (var t = con.BeginTransaction())
                {
                    try
                    {
                        var ret = con.Execute("insert into PersistedGrants (Key,ClientId,CreationTime,Data,Expiration,SubjectId,Type) values (@Key,@ClientId,@CreationTime,@Data,@Expiration,@SubjectId,@Type,)", new
                        {
                            entity.Key,
                            entity.ClientId,
                            entity.CreationTime,
                            entity.Data,
                            entity.Expiration,
                            entity.SubjectId,
                            entity.Type
                        }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                        if (ret != 1)
                        {
                            throw new Exception($"execute insert error,return values is {ret}");
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

        public void Update(PersistedGrant token)
        {
            var dbgrant = Get(token.Key);
            if (dbgrant == null)
            {
                throw new InvalidOperationException($"you can not update an notexisted PersistedGrant,key={token.Key}.");
            }
            var entity = token.ToEntity();
            using (var con = _options.DbProviderFactory.CreateConnection())
            {
                con.ConnectionString = _options.ConnectionString;
                con.Open();
                using (var t = con.BeginTransaction())
                {
                    try
                    {
                        var ret = con.Execute("update PersistedGrants" +
                            "set ClientId = @ClientId," +
                            "Data = @Data," +
                            "Expiration = @Expiration," +
                            "SubjectId = @SubjectId," +
                            "Type = @Type," +
                            "where Key = @Key" , new
                        {
                            entity.Key,
                            entity.ClientId,
                            entity.Data,
                            entity.Expiration,
                            entity.SubjectId,
                            entity.Type
                        }, commandTimeout: _options.CommandTimeOut, commandType: CommandType.Text, transaction: t);
                        if (ret != 1)
                        {
                            throw new Exception($"execute insert error,return values is {ret}");
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
    }
}
