using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer4.Dapper.Entities
{
    public class ApiSecret : Secret
    {
        public ApiResource ApiResource { get; set; }
    }
}
