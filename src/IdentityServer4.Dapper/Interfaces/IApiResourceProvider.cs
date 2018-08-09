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
    }
}
