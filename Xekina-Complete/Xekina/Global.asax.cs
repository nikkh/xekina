using Microsoft.Azure;
using Microsoft.Azure.KeyVault;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.IdentityModel.Claims;
using System.Reflection;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Xekina.Authentication;

namespace Xekina
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Trace.TraceInformation("@@ Entering method: {0}", MethodBase.GetCurrentMethod().Name);
            
           
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;
            Trace.TraceInformation("@@ Initiating data migrations");
            var configuration = new Xekina.Data.Migrations.Configuration();
            var migrator = new DbMigrator(configuration);
            migrator.Update();
            Trace.TraceInformation("@@ Completed data migrations");
            Trace.WriteLine("@@ Is this verbose?");
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(TokenHelper.GetTokenForCurrentApplication));
            Trace.TraceInformation("@@ KeyVaultClient created.  UserAgent is {0}", kv.UserAgent);
            string cacheKvUri = CloudConfigurationManager.GetSetting("XekinaRedisCacheKvUri");
            Trace.TraceInformation("@@ Uri for redis cache connection string in keyvault is {0}", cacheKvUri);
            RedisConnectorHelper.RedisConnectionString = kv.GetSecretAsync(cacheKvUri).Result.Value;
            Trace.TraceInformation("@@ First five characters of redis connection string are {0}", RedisConnectorHelper.RedisConnectionString.Substring(0,5));
            Trace.TraceInformation("@@ Leaving method: {0}", MethodBase.GetCurrentMethod().Name);
        }
    }
}
