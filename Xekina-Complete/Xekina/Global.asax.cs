using Microsoft.Azure;
using Microsoft.Azure.KeyVault;
using System.Data.Entity.Migrations;
using System.IdentityModel.Claims;
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
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;
            var configuration = new Xekina.Data.Migrations.Configuration();
            var migrator = new DbMigrator(configuration);
            migrator.Update();
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(TokenHelper.GetTokenForCurrentApplication));
            RedisConnectorHelper.RedisConnectionString = kv.GetSecretAsync(CloudConfigurationManager.GetSetting("XekinaRedisCacheKvUri")).Result.Value;

        }
    }
}
