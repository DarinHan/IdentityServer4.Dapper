using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer4.Dapper.Interfaces
{
    public interface IApiResourceProvider
    {
        ApiResource FindApiResource(string name);

        IEnumerable<ApiResource> FindApiResourcesByScope(IEnumerable<string> scopeNames);

        IEnumerable<ApiResource> FindApiResourcesAll();

        void Add(ApiResource apiResource);
        /// <summary>
        /// Update ApiResource and ApiSecrets, Scopes, Claims
        /// </summary>
        /// <param name="apiResource"></param>
        void Update(ApiResource apiResource);
        /// <summary>
        /// Update ApiSecrets by clientid
        /// </summary>
        /// <param name="apiResource"></param>
        void UpdateApiSecretsByApiResourceId(ApiResource apiResource);
        /// <summary>
        /// Update Scopes by clientid
        /// </summary>
        /// <param name="apiResource"></param>
        void UpdateScopesByApiResourceId(ApiResource apiResource);
        /// <summary>
        /// Update Claims by clientid
        /// </summary>
        /// <param name="apiResource"></param>
        void UpdateClaimsByApiResourceId(ApiResource apiResource);

        void Remove(string name);

        IEnumerable<ApiResource> Search(string keywords, int pageIndex, int pageSize, out int totalCount);

    }
}
