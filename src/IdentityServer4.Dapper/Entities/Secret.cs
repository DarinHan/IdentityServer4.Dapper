using System;
using System.Collections.Generic;
using System.Text;
using static IdentityServer4.IdentityServerConstants;

namespace IdentityServer4.Dapper.Entities
{
    public abstract class Secret
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }
        public DateTime? Expiration { get; set; }
        public string Type { get; set; } = SecretTypes.SharedSecret;
    }
}
