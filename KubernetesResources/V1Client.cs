using IdentityServer4.Models;
using k8s;
using k8s.CustomResources;
using k8s.Models;

namespace IdentityServer4K8S.KubernetesResources
{
    [KubernetesEntity(Kind = "Client", Group="identityserver.io", ApiVersion = "v1", PluralName = "clients")]
    public class V1Client : CustomResource, ISpec<Client>
    {
        public Client Spec { get; set; }
    }
}