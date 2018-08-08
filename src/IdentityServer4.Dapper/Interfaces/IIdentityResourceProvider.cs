using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer4.Dapper.Interfaces
{
    public interface IIdentityResourceProvider
    {
        IEnumerable<IdentityResource> FindIdentityResourcesByScope(IEnumerable<string> scopeNames);
        IEnumerable<IdentityResource> FindIdentityResourcesAll();
    }
}
