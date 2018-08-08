using IdentityServer4.Dapper.Options;
using IdentityServer4.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using IdentityServer4.Dapper.Interfaces;

namespace IdentityServer4.Dapper
{
    internal class TokenCleanup
    {
        private readonly ILogger<TokenCleanup> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly OperationalStoreOptions _options;

        private CancellationTokenSource _source;

        public TimeSpan CleanupInterval => TimeSpan.FromSeconds(_options.TokenCleanupInterval);

        public TokenCleanup(IServiceProvider serviceProvider, ILogger<TokenCleanup> logger, OperationalStoreOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (_options.TokenCleanupInterval < 1) throw new ArgumentException("Token cleanup interval must be at least 1 second");
            if (_options.TokenCleanupBatchSize < 1) throw new ArgumentException("Token cleanup batch size interval must be at least 1");

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void Start()
        {
            Start(CancellationToken.None);
        }

        public void Start(CancellationToken cancellationToken)
        {
            if (_source != null) throw new InvalidOperationException("Already started. Call Stop first.");

            _logger.LogDebug("Starting token cleanup");

            _source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task.Factory.StartNew(() => StartInternal(_source.Token));
        }

        public void Stop()
        {
            if (_source == null) throw new InvalidOperationException("Not started. Call Start first.");

            _logger.LogDebug("Stopping token cleanup");

            _source.Cancel();
            _source = null;
        }

        private async Task StartInternal(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("CancellationRequested. Exiting.");
                    break;
                }

                try
                {
                    await Task.Delay(CleanupInterval, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogDebug("TaskCanceledException. Exiting.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Task.Delay exception: {0}. Exiting.", ex.Message);
                    break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("CancellationRequested. Exiting.");
                    break;
                }

                ClearTokens();
            }
        }

        public void ClearTokens()
        {
            try
            {
                _logger.LogTrace("Querying for tokens to clear");

                var found = _options.TokenCleanupBatchSize;

                using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    var store = serviceScope.ServiceProvider.GetService<IPersistedGrantStoreClanup>();
                    var timestamp = DateTime.Now;
                    do
                    {
                        found = store.QueryExpired(timestamp);
                        _logger.LogInformation("Clearing {tokenCount} tokens", found);

                        if (found > 0)
                        {
                            try
                            {
                                store.RemoveRange(timestamp);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug("Concurrency exception clearing tokens: {exception}", ex.Message);
                                throw ex; //throw out to stop while loop
                            }
                        }
                    }
                    while (found > 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception clearing tokens: {exception}", ex.Message);
            }
        }
    }
}
