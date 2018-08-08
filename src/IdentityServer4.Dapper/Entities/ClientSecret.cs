using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer4.Dapper.Entities
{
    public class ClientSecret : Secret
    {
        public Client Client { get; set; }
    }
}
