using IdentityServer4.Models;
using k8s;
using k8s.CustomResources;
using k8s.Models;

namespace IdentityServer4K8S.KubernetesResources
{
    [KubernetesEntity(Kind = "IdentityResource", Group="identityserver.io", ApiVersion = "v1", PluralName = "identityresource")]
    public class V1IdentityResource : CustomResource, IKubernetesObject<V1ObjectMeta>, ISpec<IdentityResource>
    {
        public IdentityResource Spec { get; set; }
    }
}