using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using IdentityServer4.Dapper.DefaultProviders;

namespace IdentityServer4.Dapper.Interfaces
{
    public interface IClientProvider
    {
        Client FindClientById(string clientid);
        bool Exist(string clientid);

        void Add(Client client);

        IEnumerable<string> QueryAllowedCorsOrigins();

        IEnumerable<Client> Search(string keywords, int pageIndex, int pageSize, out int totalCount);



        void Remove(string clientid);

        void Update(Client client);

        void UpdateGrantTypes(Client client);
        void UpdateRedirectUris(Client client);
        void UpdatePostLogoutRedirectUris(Client client);
        void UpdateScopes(Client client);
        void UpdateSecrets(Client client);
        void UpdateClaims(Client client);
        void UpdateIdPRestrictions(Client client);
        void UpdateCorsOrigins(Client client);
        void UpdatePropertys(Client client);
    }


}
