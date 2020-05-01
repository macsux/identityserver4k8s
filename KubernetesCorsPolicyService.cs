using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4K8S.KubernetesResources;
using k8s.Informers;
using k8s.Informers.Cache;
using k8s.Informers.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityServer4K8S
{
    public class KubernetesCorsPolicyService : ICorsPolicyService
    {
        private readonly IKubernetesInformer<V1Client> _informer;
        private readonly ILogger _logger;
        private ICache<string, Client> _clients = new SimpleCache<string, Client>();
        private CompositeDisposable _subscription = new CompositeDisposable();

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityServer4.Services.InMemoryCorsPolicyService"/> class.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="clients">The clients.</param>
        public KubernetesCorsPolicyService(IKubernetesInformer<V1Client> informer, ILogger<KubernetesCorsPolicyService> logger)
        {
            _informer = informer;
            _logger = logger;
            _informer.GetResource(ResourceStreamType.ListWatch)
                .Where(x => x.Value != null)
                .Select(x => x.Value.Spec.ToResourceEvent(x.EventFlags))
                .SynchronizeCache(_clients, x => x.ClientId)
                .Subscribe()
                .DisposeWith(_subscription);
        }

        /// <summary>
        /// Determines whether origin is allowed.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <returns></returns>
        public virtual Task<bool> IsOriginAllowedAsync(string origin)
        {
            var query = _clients.Values.SelectMany(x => x.AllowedCorsOrigins);

            var result = query.Contains(origin, StringComparer.OrdinalIgnoreCase);

            if (result)
            {
                _logger.LogDebug("Client list checked and origin: {0} is allowed", origin);
            }
            else
            {
                _logger.LogDebug("Client list checked and origin: {0} is not allowed", origin);
            }

            return Task.FromResult(result);
        }
    }
}