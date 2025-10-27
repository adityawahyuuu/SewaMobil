using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Data.Entity;
using SewaMobil.Models;

namespace SewaMobil
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default",
                "{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            // Initialize Database with SQL Server Compact
            Database.SetInitializer(new CarRentalInitializer());

            // Create database if not exists
            using (var context = new CarRentalContext())
            {
                context.Database.Initialize(force: false);
            }
        }

        protected void Application_Error()
        {
            var exception = Server.GetLastError();

            // Log error
            System.Diagnostics.Debug.WriteLine(string.Format("Application Error: {0}",
                exception != null ? exception.Message : "Unknown error"));

            // In development, show error page
            if (HttpContext.Current != null && HttpContext.Current.IsDebuggingEnabled)
            {
                return;
            }

            Server.ClearError();
            Response.Redirect("/Home/Error");
        }
    }
}