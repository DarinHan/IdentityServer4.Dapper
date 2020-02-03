using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer4.Dapper.Entities
{
    public class ClientScope
    {
        public int Id { get; set; }
        public string Scope { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public Client Client { get; set; }
    }
}
