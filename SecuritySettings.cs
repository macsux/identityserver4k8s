using System.Collections.Generic;
using IdentityServer4.Models;
using IdentityServer4.Test;

namespace IdentityServer4K8S
{
    public class SecuritySettings
    {
        public List<Client> Clients { get; set; } = new List<Client>();
        public List<ApiResource> ApiResources { get; set; } = new List<ApiResource>();
        public List<IdentityResource> IdentityResources { get; set; } = new List<IdentityResource>();
        public List<TestUser> Users { get; set; } = new List<TestUser>();
    }
}