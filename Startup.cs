using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Validation;
using informers;
using k8s;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IdentityServer4K8S
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddIdentityServer()
                .AddResourceStore<KubernetesResourceStore>()
                .AddClientStore<KubernetesClientStore>()
                .AddProfileService<KubernetesUserProfileService>()
                .AddResourceOwnerValidator<KubernetesUserResourceOwnerPasswordValidator>()
                .AddSecretValidator<PlainTextSharedSecretValidator>()
                // .AddExtensionGrantValidator<DelegationGrantValidator>()
                // .AddExtensionGrantValidator<TokenExchangeGrantValidator>()
                .AddDeveloperSigningCredential();
            services.AddIdentityServerKubernetesResources();
            services.AddControllers();
            services.AddKubernetesClient(KubernetesClientConfiguration.BuildDefaultConfig);
            services.AddKubernetesInformers();
            services.AddHostedService<AppIdentityController>();
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            // app.UseAuthorization();
            app.UseIdentityServer();
            app.UseStaticFiles();
            // app.UseMvcWithDefaultRoute();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            
            
            
        }
    }
}
