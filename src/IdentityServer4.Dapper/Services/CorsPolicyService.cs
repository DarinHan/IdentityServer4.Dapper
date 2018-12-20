using IdentityServer4.Dapper.Interfaces;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer4.Dapper.Services
{
    public class CorsPolicyService : ICorsPolicyService
    {
        private readonly IClientProvider _clientprovider;
        private readonly ILogger<CorsPolicyService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorsPolicyService"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">context</exception>
        public CorsPolicyService(IClientProvider provider, ILogger<CorsPolicyService> logger)
        {
            _clientprovider = provider ?? throw new ArgumentNullException(nameof(provider));
            _logger = logger;
        }

        public Task<bool> IsOriginAllowedAsync(string origin)
        {
            var distinctOrigins = _clientprovider.QueryAllowedCorsOrigins();

            var isAllowed = distinctOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
            _logger.LogDebug($"Origin {origin} is allowed: {isAllowed}");

            return Task.FromResult(isAllowed);
        }
    }
}
