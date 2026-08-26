using System;
using System.Net;
using System.Web.Helpers;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    public class ValidateHeaderAntiForgeryTokenAttribute : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException(nameof(filterContext));

            var request = filterContext.HttpContext.Request;
            string cookieToken = request.Cookies[AntiForgeryConfig.CookieName]?.Value ?? string.Empty;
            string headerToken = request.Headers["X-CSRF-Token"] ?? string.Empty;
            try
            {
                AntiForgery.Validate(cookieToken, headerToken);
            }
            catch (HttpAntiForgeryException ex)
            {
                filterContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
                filterContext.Result = new ContentResult { Content = ex.Message };
            }
        }
    }
}