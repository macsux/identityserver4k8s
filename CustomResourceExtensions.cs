using System.Collections;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IdentityServer4K8S
{
    public static class CustomResourceExtensions
    {
        public static void AddIdentityServerKubernetesResources(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IHostedService, CustomResourceInstaller>();
        }
    }
}