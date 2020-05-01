using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Test;
using IdentityServer4.Validation;
using IdentityServer4K8S.KubernetesResources;
using k8s.Informers;
using k8s.Informers.Cache;
using k8s.Informers.Notifications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace IdentityServer4K8S
{
    public class KubernetesUserResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private ICache<string, TestUser> _users = new SimpleCache<string, TestUser>();
        private CompositeDisposable _subscription = new CompositeDisposable();

        private readonly ISystemClock _clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityServer4.Test.TestUserResourceOwnerPasswordValidator"/> class.
        /// </summary>
        /// <param name="users">The users.</param>
        /// <param name="clock">The clock.</param>
        public KubernetesUserResourceOwnerPasswordValidator(IKubernetesInformer<V1TestUser> informer, ISystemClock clock)
        {
            informer.GetResource(ResourceStreamType.ListWatch, KubernetesInformerOptions.Default)
                .Select(x => new ResourceEvent<TestUser>(x.EventFlags, x.Value?.Spec))
                .SynchronizeCache(_users, x => x.SubjectId)
                .Subscribe()
                .DisposeWith(_subscription);
            _clock = clock;
        }

        /// <summary>
        /// Validates the resource owner password credential
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            var store = new TestUserStore(_users.Values.ToList());
            if (store.ValidateCredentials(context.UserName, context.Password))
            {
                var user = store.FindByUsername(context.UserName);
                context.Result = new GrantValidationResult(
                    user.SubjectId ?? throw new ArgumentException("Subject ID not set", nameof(user.SubjectId)),
                    OidcConstants.AuthenticationMethods.Password, _clock.UtcNow.UtcDateTime,
                    user.Claims);
            }

            return Task.CompletedTask;
        }
    }
}