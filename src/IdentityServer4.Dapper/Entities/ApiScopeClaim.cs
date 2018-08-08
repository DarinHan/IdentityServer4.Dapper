using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer4.Dapper.Entities
{
    public class ApiScopeClaim : UserClaim
    {
        public ApiScope ApiScope { get; set; }
    }
}
