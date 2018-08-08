using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer4.Dapper.Entities
{
    public class ClientGrantType
    {
        public int Id { get; set; }
        public string GrantType { get; set; }
        public Client Client { get; set; }
    }
}
