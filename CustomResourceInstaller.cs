using System.Threading;
using System.Threading.Tasks;
using IdentityServer4K8S.KubernetesResources;
using k8s;
using k8s.CustomResources;
using k8s.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;

namespace IdentityServer4K8S
{
    public class CustomResourceInstaller : IHostedService
    {
        private readonly IKubernetes _client;
        private readonly ILogger _logger;

        public CustomResourceInstaller(IKubernetes client, ILogger<CustomResourceInstaller> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {

            
                var clientRegistration = V1CustomResourceDefinition.Builder
                    .SetScope(Scope.Namespaced)
                    .AddVersion<V1Client>()
                    .IsServe()
                    .IsStore()
                    .Build();
                var apiResourceRegistration = V1CustomResourceDefinition.Builder
                    .SetScope(Scope.Namespaced)
                    .AddVersion<V1ApiResource>()
                    .IsServe()
                    .IsStore()
                    .Build();
                var identityResourceRegistration = V1CustomResourceDefinition.Builder
                    .SetScope(Scope.Namespaced)
                    .AddVersion<V1IdentityResource>()
                    .IsServe()
                    .IsStore()
                    .Build();
                var testUserRegistration = V1CustomResourceDefinition.Builder
                    .SetScope(Scope.Namespaced)
                    .AddVersion<V1TestUser>()
                    .IsServe()
                    .IsStore()
                    .Build();
                _logger.LogInformation("Installing CRDs...");

                await _client.UnInstallCustomResourceDefinition(clientRegistration);
                await _client.UnInstallCustomResourceDefinition(apiResourceRegistration);
                await _client.UnInstallCustomResourceDefinition(identityResourceRegistration);
                await _client.UnInstallCustomResourceDefinition(testUserRegistration);
                
                await _client.InstallCustomResourceDefinition(clientRegistration);
                await _client.InstallCustomResourceDefinition(apiResourceRegistration);
                await _client.InstallCustomResourceDefinition(identityResourceRegistration);
                await _client.InstallCustomResourceDefinition(testUserRegistration);
                _logger.LogInformation("CRD installed!");
            }
            catch (HttpOperationException e)
            {
                _logger.LogCritical(e, e.Response.Content);
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}