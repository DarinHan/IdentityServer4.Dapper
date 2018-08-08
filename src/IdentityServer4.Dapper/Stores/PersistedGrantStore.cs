using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IdentityServer4.Dapper.Interfaces;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Dapper.Stores
{
    /// <summary>
    /// Implementation of IPersistedGrantStore thats uses Dapper.
    /// </summary>
    /// <seealso cref="IdentityServer4.Stores.IPersistedGrantStore" />
    public class PersistedGrantStore : IPersistedGrantStore
    {
        private readonly IPersistedGrantProvider _persistedgrantprovider;
        private readonly ILogger<ClientStore> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistedGrantStore"/> class.
        /// </summary>
        /// <param name="persistedGrantProvider">the provider.</param>
        /// <param name="logger">the logger.</param>
        public PersistedGrantStore(IPersistedGrantProvider persistedGrantProvider, ILogger<ClientStore> logger)
        {
            _persistedgrantprovider = persistedGrantProvider ?? throw new ArgumentNullException(nameof(persistedGrantProvider));
            _logger = logger;
        }

        public Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var results = _persistedgrantprovider.GetAll(subjectId);
            _logger.LogDebug("{persistedGrantCount} persisted grants found for {subjectId}", results.Count(), subjectId);

            return Task.FromResult(results);
        }

        public Task<PersistedGrant> GetAsync(string key)
        {
            var result = _persistedgrantprovider.Get(key);
            _logger.LogDebug("{persistedGrantKey} found in database: {persistedGrantKeyFound}", key, result != null);
            return Task.FromResult(result);
        }

        public Task RemoveAllAsync(string subjectId, string clientId)
        {
            var persistedGrants = _persistedgrantprovider.GetAll(subjectId, clientId).AsEnumerable();
            _logger.LogDebug("removing {persistedGrantCount} persisted grants from database for subject {subjectId}, clientId {clientId}", persistedGrants.Count(), subjectId, clientId);

            try
            {
                _persistedgrantprovider.RemoveAll(subjectId, clientId);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("removing {persistedGrantCount} persisted grants from database for subject {subjectId}, clientId {clientId}: {error}", persistedGrants.Count(), subjectId, clientId, ex.Message);
            }
            return Task.FromResult(0);
        }

        public Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            var persistedGrants = _persistedgrantprovider.GetAll(subjectId, clientId, type).AsEnumerable();
            _logger.LogDebug("removing {persistedGrantCount} persisted grants from database for subject {subjectId}, clientId {clientId}, grantType {persistedGrantType}", persistedGrants.Count(), subjectId, clientId, type);

            try
            {
                _persistedgrantprovider.RemoveAll(subjectId, clientId, type);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("exception removing {persistedGrantCount} persisted grants from database for subject {subjectId}, clientId {clientId}, grantType {persistedGrantType}: {error}", persistedGrants.Count(), subjectId, clientId, type, ex.Message);
            }
            return Task.FromResult(0);
        }

        public Task RemoveAsync(string key)
        {
            var persistedGrant = _persistedgrantprovider.Get(key);
            if (persistedGrant != null)
            {
                _logger.LogDebug("removing {persistedGrantKey} persisted grant from database", key);
                try
                {
                    _persistedgrantprovider.Remove(key);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("exception removing {persistedGrantKey} persisted grant from database: {error}", key, ex.Message);
                }
            }
            else
            {
                _logger.LogDebug("no {persistedGrantKey} persisted grant found in database", key);
            }
            return Task.FromResult(0);
        }

        public Task StoreAsync(PersistedGrant token)
        {
            var existing = _persistedgrantprovider.Get(token.Key);
            try
            {
                if (existing == null)
                {
                    _logger.LogDebug("{persistedGrantKey} not found in database", token.Key);
                        
                    _persistedgrantprovider.Add(token);
                }
                else
                {
                    _logger.LogDebug("{persistedGrantKey} found in database", token.Key);

                    _persistedgrantprovider.Update(token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("exception updating {persistedGrantKey} persisted grant in database: {error}", token.Key, ex.Message);
            }

            return Task.FromResult(0);
        }
    }
}
