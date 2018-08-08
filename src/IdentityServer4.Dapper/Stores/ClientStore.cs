using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IdentityServer4.Dapper.Interfaces;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Dapper.Stores
{
    public class ClientStore : IClientStore
    {
        private readonly IClientProvider _clientDB;
        private readonly ILogger<ClientStore> _logger;

        public ClientStore(IClientProvider client, ILogger<ClientStore> logger)
        {
            _clientDB = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger;
        }

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            var client = _clientDB.FindClientById(clientId);

            _logger.LogDebug("{clientId} found in database: {clientIdFound}", clientId, client != null);
            return Task.FromResult<Client>(client);
        }
    }
}
