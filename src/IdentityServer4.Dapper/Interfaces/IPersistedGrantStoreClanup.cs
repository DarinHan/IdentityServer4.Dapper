using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer4.Dapper.Interfaces
{
    public interface IPersistedGrantStoreClanup
    {
        int QueryExpired(DateTime dateTime);
        void RemoveRange(DateTime dateTime);
    }
}
