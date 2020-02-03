using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer4.Dapper.Entities
{
    public class ClientSecret : Secret
    {
        [Newtonsoft.Json.JsonIgnore]
        public Client Client { get; set; }
    }
}
