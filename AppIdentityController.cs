using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4K8S.KubernetesResources;
using k8s;
using k8s.Informers;
using k8s.Informers.Cache;
using k8s.Informers.Notifications;
using k8s.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IdentityServer4K8S
{
    public class AppIdentityController : IHostedService, IReconciler<string, V1Pod>
    {
        private readonly IKubernetes _client;
        private readonly IKubernetesInformer<V1Pod> _podInformer;
        private readonly IKubernetesInformer<V1ConfigMap> _configMapInformer;
        private readonly IKubernetesInformer<V1ApiResource> _apiResourceInformer;
        private readonly IKubernetesInformer<V1Client> _clientInformer;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private CompositeDisposable _subscription = new CompositeDisposable();
        private ICache<string, V1ConfigMap> _configMaps = new SimpleCache<string, V1ConfigMap>();
        private ICache<string, V1ApiResource> _apiResources = new SimpleCache<string, V1ApiResource>();
        private ICache<string, V1Client> _clients = new SimpleCache<string, V1Client>();
        private const string SpringApplicationName = "spring.application.name";
        private const string Oauth2ClientType = "oauth2.client.type";

        public AppIdentityController(
            IKubernetes client, 
            IKubernetesInformer<V1Pod> podInformer, 
            IKubernetesInformer<V1ConfigMap> configMapInformer, 
            IKubernetesInformer<V1ApiResource> apiResourceInformer, 
            IKubernetesInformer<V1Client> clientInformer, 
            
            ILoggerFactory loggerFactory)
        {
            _client = client;
            _podInformer = podInformer;
            _configMapInformer = configMapInformer;
            _apiResourceInformer = apiResourceInformer;
            _clientInformer = clientInformer;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<CustomResourceInstaller>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var configMapSynchronized = new TaskCompletionSource<bool>();
            var apiResourceSynchronized = new TaskCompletionSource<bool>();
            var clientSynchronized = new TaskCompletionSource<bool>();

            _configMapInformer.GetResource(ResourceStreamType.ListWatch, KubernetesInformerOptions.Builder.HasLabel(SpringApplicationName).HasLabel(Oauth2ClientType).Build())
                .SynchronizeCache(_configMaps, x => x.Metadata.Name)
                .Do(x => configMapSynchronized.TrySetResult(true))
                .Subscribe()
                .DisposeWith(_subscription);
            _apiResourceInformer.GetResource(ResourceStreamType.ListWatch)
                .SynchronizeCache(_apiResources, x => x.Metadata.Name)
                .Do(x => apiResourceSynchronized.TrySetResult(true))
                .Subscribe()
                .DisposeWith(_subscription);

            _clientInformer.GetResource(ResourceStreamType.ListWatch)
                .SynchronizeCache(_clients, x => x.Metadata.Name)
                .Do(x => clientSynchronized.TrySetResult(true))
                .Subscribe()
                .DisposeWith(_subscription);

            await Task.WhenAll(configMapSynchronized.Task, apiResourceSynchronized.Task, clientSynchronized.Task);
            
            _podInformer.GetResource(ResourceStreamType.ListWatch, KubernetesInformerOptions.Builder.HasLabel(SpringApplicationName).Build())
                .ReconcileWith(this, _loggerFactory)
                .DisposeWith(_subscription);
        }
        
        


        public Task StopAsync(CancellationToken cancellationToken)
        {
            _subscription.Dispose();
            return Task.CompletedTask;
        }

        public async Task Reconcile(ReconilationContext<string, V1Pod> context, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"========= {context.ResourceId} ============");
            var pod = context.LastKnownState;
            var springAppName = pod.GetLabel(SpringApplicationName);
            // var claims = (IList<string>)pod.Metadata.Annotations?.Keys
            //     .Select(x => x.Split("."))
            //     .Where(x => x.Length == 2 && x[0] == "claim")
            //     .Select(x =>
            //     {
            //         var items = x.Split(".");
            //         return new
            //         {
            //             Resource = items[1]
            //         }
            //     })
            //     .ToList() ?? Array.Empty<string>();
            // var claimsToCreate = claims.Select(resourceName => new V1ApiResource()
            // {
            //     Metadata =
            //     {
            //         Name = 
            //     }
            // })
            
            
            var ns = pod.Namespace();
            if (!_clients.TryGetValue(springAppName, out var oauthClient))
            {
                oauthClient = new V1Client()
                {
                    Metadata =
                    {
                        Name = $"{springAppName}"
                    },
                    Spec = new Client()
                    {
                        ClientId = springAppName,
                        ClientSecrets = new List<Secret>()
                        {
                            new Secret(Guid.NewGuid().ToString("N"))
                        },
                        AllowedGrantTypes = new List<string> {"client_credentials"}
                    }
                };
                var response = await _client.CreateWithHttpMessagesAsync(oauthClient, ns, cancellationToken: cancellationToken);
                oauthClient = response.Body;
            }

            var configMapName = $"{springAppName}.oauth2.client";
            // var configMap = _configMaps.Values.FirstOrDefault(x => x.Metadata.Labels.TryGetValue(configMapName, out var actualName) && actualName == springAppName);
            if (!_configMaps.TryGetValue(configMapName, out var configMap))
            {
                configMap = new V1ConfigMap().Initialize();
                configMap.Metadata.Name = configMapName;
                configMap.SetLabel(Oauth2ClientType, "client_credentials");
                configMap.SetLabel(SpringApplicationName, springAppName);
                configMap.Data = new Dictionary<string, string>
                {
                    { "oauth2.client.clientId", springAppName },
                    { "oauth2.client.secret", oauthClient.Spec.ClientSecrets.First().Value },
                };
                var createConfigMapResponse = await _client.CreateWithHttpMessagesAsync(configMap, ns, cancellationToken: cancellationToken);
                configMap = createConfigMapResponse.Body;
            }
        }
        public bool ShouldIntterupt(ResourceEvent<V1Pod> @event) => false;
    }
}