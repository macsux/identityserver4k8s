using IdentityServer4.Models;
using k8s;
using k8s.CustomResources;
using k8s.Models;

namespace IdentityServer4K8S.KubernetesResources
{
    [KubernetesEntity(Kind = "ApiResource", Group="identityserver.io", ApiVersion = "v1", PluralName = "apiresource")]
    public class V1ApiResource : CustomResource, IKubernetesObject<V1ObjectMeta>, ISpec<ApiResource>
    {
        public ApiResource Spec { get; set; }
    }
}