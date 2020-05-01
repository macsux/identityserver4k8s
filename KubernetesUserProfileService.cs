using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Test;
using IdentityServer4K8S.KubernetesResources;
using k8s.Informers;
using k8s.Informers.Cache;
using k8s.Informers.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityServer4K8S
{
    public class KubernetesUserProfileService : IProfileService
    {
        private ICache<string, TestUser> _users = new SimpleCache<string, TestUser>();

        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        private CompositeDisposable _subscription = new CompositeDisposable();

        public KubernetesUserProfileService(IKubernetesInformer<V1TestUser> informer, ILogger<KubernetesUserProfileService> logger)
        {
            informer.GetResource(ResourceStreamType.ListWatch, KubernetesInformerOptions.Default)
                .Select(x => new ResourceEvent<TestUser>(x.EventFlags, x.Value?.Spec))
                .SynchronizeCache(_users, x => x.SubjectId)
                .Subscribe()
                .DisposeWith(_subscription);
            Logger = logger;
        }

        /// <summary>
        /// This method is called whenever claims about the user are requested (e.g. during token creation or via the userinfo endpoint)
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public virtual Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            context.LogProfileRequest(Logger);

            if (context.RequestedClaimTypes.Any())
            {
                var user = new TestUserStore(_users.Values.ToList()).FindBySubjectId(context.Subject.GetSubjectId());
                if (user != null)
                {
                    context.AddRequestedClaims(user.Claims);
                }
            }

            context.IssuedClaims = context.Subject.Claims.ToList();
            context.LogIssuedClaims(Logger);

            return Task.CompletedTask;
        }

        /// <summary>
        /// This method gets called whenever identity server needs to determine if the user is valid or active (e.g. if the user's account has been deactivated since they logged in).
        /// (e.g. during token issuance or validation).
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public virtual Task IsActiveAsync(IsActiveContext context)
        {
            Logger.LogDebug("IsActive called from: {caller}", context.Caller);

            var user = new TestUserStore(_users.Values.ToList()).FindBySubjectId(context.Subject.GetSubjectId());
            context.IsActive = user?.IsActive == true;

            return Task.CompletedTask;
        }
    }
}