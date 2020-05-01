using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4K8S.KubernetesResources;
using k8s.Informers;
using k8s.Informers.Cache;
using k8s.Informers.Notifications;
using Microsoft.Extensions.Options;

namespace IdentityServer4K8S
{
    public class KubernetesResourceStore : IResourceStore
    {
        private readonly IKubernetesInformer<V1ApiResource> _apiInformer;
        private readonly IKubernetesInformer<V1IdentityResource> _identityInformer;
        private CompositeDisposable _subscription = new CompositeDisposable();
        private ICache<string, ApiResource> _apiResources = new SimpleCache<string, ApiResource>();
        private ICache<string, IdentityResource> _identityResources = new SimpleCache<string, IdentityResource>();


        
        public KubernetesResourceStore(IKubernetesInformer<V1IdentityResource> identityInformer, IKubernetesInformer<V1ApiResource> apiInformer)
        {
            _identityInformer = identityInformer;
            _apiInformer = apiInformer;

            _apiInformer.GetResource(ResourceStreamType.ListWatch)
                .Select(x => new ResourceEvent<ApiResource>(x.EventFlags, x.Value?.Spec))
                .SynchronizeCache(_apiResources, x => x.Name)
                .Subscribe()
                .DisposeWith(_subscription);
            
            _identityInformer.GetResource(ResourceStreamType.ListWatch)
                .Select(x => new ResourceEvent<IdentityResource>(x.EventFlags, x.Value?.Spec))
                .SynchronizeCache(_identityResources, x => x.Name)
                .Subscribe()
                .DisposeWith(_subscription);

        }
        /// <summary>
        /// Gets all resources.
        /// </summary>
        /// <returns></returns>
        public Task<IdentityServer4.Models.Resources> GetAllResourcesAsync()
        {
            var result = new IdentityServer4.Models.Resources
            {
                ApiResources = _apiResources.Values.ToList(),
                IdentityResources = _identityResources.Values.ToList()
            };
            return Task.FromResult(result);
        }

        /// <summary>
        /// Finds the API resource by name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public Task<ApiResource> FindApiResourceAsync(string name)
        {
            _apiResources.TryGetValue(name, out var resource);
            return Task.FromResult(resource);
        }

        /// <summary>
        /// Finds the identity resources by scope.
        /// </summary>
        /// <param name="names">The names.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">names</exception>
        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> names)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));

            
            var identity = from i in _identityResources.Values
                where names.Contains(i.Name)
                select i;

            return Task.FromResult(identity);
        }

        /// <summary>
        /// Finds the API resources by scope.
        /// </summary>
        /// <param name="names">The names.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">names</exception>
        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> names)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));

            var api = from a in _apiResources.Values
                let scopes = (from s in a.Scopes where names.Contains(s.Name) select s)
                where scopes.Any()
                select a;

            return Task.FromResult(api);
        }
    }
}