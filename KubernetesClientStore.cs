using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4K8S.KubernetesResources;
using k8s;
using k8s.Informers;
using k8s.Informers.Cache;
using k8s.Informers.Notifications;
using Microsoft.Extensions.Options;

namespace IdentityServer4K8S
{
    public class KubernetesClientStore : IClientStore
    {
        private readonly IKubernetesInformer<V1Client> _informer;
        private ICache<string, Client> _clients = new SimpleCache<string, Client>();
        private CompositeDisposable _subscription = new CompositeDisposable();

        public KubernetesClientStore(IKubernetesInformer<V1Client> informer)
        {
            _informer = informer;
            _informer.GetResource(ResourceStreamType.ListWatch, KubernetesInformerOptions.Default)
                .Select(x => new ResourceEvent<Client>(x.EventFlags, x.Value?.Spec))
                .SynchronizeCache(_clients, x => x.ClientId)
                .Subscribe()
                .DisposeWith(_subscription);
        }

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            _clients.TryGetValue(clientId, out var client);
            return Task.FromResult(client);
        }
    }
}