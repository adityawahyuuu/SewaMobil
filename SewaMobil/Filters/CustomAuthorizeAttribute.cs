using System;
using System.Web;
using System.Web.Mvc;

namespace SewaMobil.Filters
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        public string RequiredRole { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext.Session["UserId"] == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(RequiredRole))
            {
                var userRole = httpContext.Session["UserRole"].ToString();
                if (userRole != RequiredRole)
                {
                    return false;
                }
            }

            return true;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.Session["UserId"] == null)
            {
                filterContext.Result = new RedirectResult("~/Account/Login");
            }
            else
            {
                filterContext.Result = new RedirectResult("~/Home/Index");
            }
        }
    }
}