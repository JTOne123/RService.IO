﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RService.IO.Abstractions
{
    /// <summary>
    /// Determines if a service endpoint is authenticated and authorized to be executed.
    /// </summary>
    /// <remarks>
    /// These methods should be used prior to executing the service. <br/><br/>
    /// Also if there are more than one Authorization attribute on a class and/or
    /// method then all authorizations must pass.  If there is only one
    /// Authorization attribute with a comma separated list then only one attribute
    /// must be satisfied.
    /// </remarks>
    public interface IAuthProvider
    {
        /// <summary>
        /// Checks AuthN of the request.
        /// </summary>
        /// <param name="ctx">The <see cref="HttpContext"/> of the request.</param>
        /// <returns><b>True</b> if the request is authenticated, else <b>False</b>.</returns>
        bool IsAuthenticated(HttpContext ctx);

        /// <summary>
        /// Checks AuthZ on a given endpoint.
        /// </summary>
        /// <param name="ctx">The <see cref="HttpContext"/> of the request.</param>
        /// <param name="authorizationFilters">A collection of "authorized"/"allow anonymous" attributes.</param>
        /// <returns><b>True</b> if the user is authorized for the given endpoint, else <b>False</b>.</returns>
        /// <remarks>
        /// All attributes on an endpoint and class must evaluate to <b>True</b> for this to return <b>True</b>.
        /// </remarks>
        Task<bool> IsAuthorizedAsync(HttpContext ctx, IEnumerable<object> authorizationFilters);

        /// <summary>
        /// If AuthZ failed this will challenge the user for new AuthZ.
        /// </summary>
        /// <param name="ctx">The <see cref="HttpContext"/> of the request.</param>
        /// <param name="authorizationFilters">A collection of "authorized"/"allow anonymous" attributes.</param>
        /// <returns>
        /// A <see cref="Task"/> that on completion indicates the filter has executed.
        /// </returns>
        Task OnChallengeAuthorizationAsync(HttpContext ctx, IEnumerable<object> authorizationFilters);
    }
}