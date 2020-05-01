using IdentityServer4.Test;
using k8s;
using k8s.CustomResources;
using k8s.Models;

namespace IdentityServer4K8S.KubernetesResources
{
    [KubernetesEntity(Kind = "TestUser", Group="identityserver.io", ApiVersion = "v1", PluralName = "testuser")]
    public class V1TestUser : CustomResource, IKubernetesObject<V1ObjectMeta>, ISpec<TestUser>
    {
        public TestUser Spec { get; set; }
    }
}