using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Filters;
using GorillaGolfCommon.core;

namespace GorillaGolfWeb.Filters
{
    public class AdminAuthentication : ActionFilterAttribute, IAuthenticationFilter
    {
        public void OnAuthentication(AuthenticationContext filterContext)
        {
            // If user is not on session or not an admin user, redirect to Login
            if (SessionVariables.PlayerID == null || !SessionVariables.IsAdmin)
            {
                filterContext.Result = new RedirectResult("/login");
            }
        }

        public void OnAuthenticationChallenge(AuthenticationChallengeContext filterContext)
        {
            if (filterContext.Result == null || filterContext.Result is HttpUnauthorizedResult)
            {
                filterContext.Result = new RedirectResult("/login");
            }
        }
    }
}