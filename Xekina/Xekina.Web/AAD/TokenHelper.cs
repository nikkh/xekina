using System.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Xekina.Web.Models;

namespace Xekina.Web.AAD
{
    public class TokenHelper
    {
        public async Task<string> GetTokenForCurrentUser()
        {
            string clientId = ConfigurationManager.GetSetting("ida:ClientId");
            string clientSecret = CloudConfigurationManager.GetSetting("ida:ClientSecret");
            string tenantId = CloudConfigurationManager.GetSetting("ida:TenantId");
            string aadInstance = CloudConfigurationManager.GetSetting("ida:AADInstance");
            string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            string azureResourceManager = @"https://management.azure.com/";

            // get a token for the Graph without triggering any user interaction (from the cache, via multi-resource refresh token, etc)
            ClientCredential clientcred = new ClientCredential(clientId, clientSecret);
            // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's database
            AuthenticationContext authenticationContext = new AuthenticationContext(aadInstance + tenantID, new ADALTokenCache(signedInUserID));
            AuthenticationResult authenticationResult = authenticationContext.AcquireToken(azureResourceManager, clientcred);
            return authenticationResult.AccessToken;
        }
    }
}