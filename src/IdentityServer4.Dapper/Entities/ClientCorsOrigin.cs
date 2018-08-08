using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer4.Dapper.Entities
{
    public class ClientCorsOrigin
    {
        public int Id { get; set; }
        public string Origin { get; set; }
        public Client Client { get; set; }
    }
}
