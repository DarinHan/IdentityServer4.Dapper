using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IdentityServer4.Dapper.Interfaces;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace IdentityServer4.Dapper.Stores
{
    public class ClientStore : IClientStore
    {
        private readonly IClientProvider _clientDB;
        private readonly ILogger<ClientStore> _logger;

        private readonly IMemoryCache _memoryCache;

        private static volatile object locker = new object();

        public ClientStore(IClientProvider client, ILogger<ClientStore> logger, IMemoryCache memoryCache)
        {
            _clientDB = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            var client = _memoryCache.Get<Client>("clients." + clientId);

            if (client == null)
            {
                lock (locker)
                {
                    client = _memoryCache.Get<Client>("clients." + clientId);
                    if (client != null)
                    {
                        return Task.FromResult<Client>(client);
                    }

                    client = _clientDB.FindClientById(clientId);
                    _logger.LogDebug("{clientId} found in database: {clientIdFound}", clientId, client != null);

                    if (client != null)
                    {
                        _memoryCache.Set<Client>("clients." + clientId, client, TimeSpan.FromMinutes(5));
                    }
                }
            }

            return Task.FromResult<Client>(client);
        }
    }
}
