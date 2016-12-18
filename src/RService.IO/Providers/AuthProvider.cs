﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using RService.IO.Abstractions;

namespace RService.IO.Providers
{
    /// <summary>
    /// Default implementation of <see cref="IAuthProvider"/>.
    /// </summary>
    public class AuthProvider : IAuthProvider
    {
        private readonly IAuthorizationPolicyProvider _policyProvider;

        /// <summary>
        ///
        /// </summary>
        /// <param name="provider"></param>
        public AuthProvider(IAuthorizationPolicyProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            _policyProvider = provider;
        }

        /// <inheritdoc/>
        public bool IsAuthenticated(HttpContext ctx)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<bool> IsAuthorizedAsync(HttpContext ctx, IEnumerable<object> authorizationFilters)
        {
            var results = await AuthorizedAsync(ctx, authorizationFilters);
            return results.IsAuthorized;
        }

        /// <inheritdoc/>
        public async Task OnAuthorizationAsync(HttpContext ctx, IEnumerable<object> authorizationFilters)
        {
            var response = await AuthorizedAsync(ctx, authorizationFilters);
            if (!response.IsAuthorized)
            {
                if (response.AuthenticationSchemes?.Any() ?? false)
                {
                    foreach (var scheme in response.AuthenticationSchemes)
                    {
                        await ctx.Authentication.ChallengeAsync(scheme);
                    }
                }
                else
                {
                    await ctx.Authentication.ChallengeAsync((AuthenticationProperties) null);
                }
            }
        }

        private async Task<ChallangeResponse> AuthorizedAsync(HttpContext ctx, IEnumerable<object> authorizationFilters)
        {
            if (ctx == null)
                throw new ArgumentNullException(nameof(ctx));
            if (authorizationFilters == null)
                throw new ArgumentNullException(nameof(authorizationFilters));

            var authFilters = authorizationFilters.ToList();

            // Allow Anonymous skips all authorization
            if (authFilters.Any(x => x is IAllowAnonymous))
                return new ChallangeResponse {IsAuthorized = true};

            // Get the authorization policy
            var authData = authFilters.Where(x => x is IAuthorizeData).Cast<IAuthorizeData>().ToList();
            var effectivePolicy = await AuthorizationPolicy.CombineAsync(_policyProvider, authData);

            if (effectivePolicy == null)
                return new ChallangeResponse {IsAuthorized = true};

            // Build a ClaimsPrincipal with the policy's required authentication types
            if (effectivePolicy.AuthenticationSchemes?.Any() ?? false)
            {
                ClaimsPrincipal newPrincipal = null;
                foreach (var scheme in effectivePolicy.AuthenticationSchemes)
                {
                    var result = await ctx.Authentication.AuthenticateAsync(scheme);
                    if (result != null)
                    {
                        newPrincipal = SecurityHelper.MergeUserPrincipal(newPrincipal, result);
                    }
                }
                // If all schemes failed authentication, provide a default identity anyways
                ctx.User = newPrincipal ?? new ClaimsPrincipal(new ClaimsIdentity());
            }

            var authService = ctx.RequestServices.GetRequiredService<IAuthorizationService>();

            // Note: Default Anonymous User is new ClaimsPrincipal(new ClaimsIdentity())
            var authoized = await authService.AuthorizeAsync(ctx.User, effectivePolicy);
            return new ChallangeResponse
            {
                IsAuthorized = authoized,
                AuthenticationSchemes = effectivePolicy.AuthenticationSchemes
            };
        }

        private struct ChallangeResponse
        {
            public bool IsAuthorized;
            public IReadOnlyList<string> AuthenticationSchemes;
        }
    }
}