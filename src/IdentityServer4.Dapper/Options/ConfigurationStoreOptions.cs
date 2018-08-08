using System;
using System.Collections.Generic;
using System.Text;
using IdentityServer4.Dapper.Interfaces;

namespace IdentityServer4.Dapper.Options
{
    /// <summary>
    /// Options for configuring the configuration context.
    /// </summary>
    public class ConfigurationStoreOptions
    {
        /// <summary>
        /// ApiResource Provider
        /// </summary>
        public IApiResourceProvider ApiResourceProvidor { get; set; }

        /// <summary>
        /// IdentityResource Provider
        /// </summary>
        public IIdentityResourceProvider IdentityResourceProvidor { get; set; }

        /// <summary>
        /// ClientResource Provider
        /// </summary>
        public IClientProvider ClientResourceProvidor { get; set; }

    }
}
