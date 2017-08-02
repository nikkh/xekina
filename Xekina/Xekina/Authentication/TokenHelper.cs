using System.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure;
using Xekina.Models;

namespace Xekina.Authentication
{
    public static class TokenHelper
    {
        public static async Task<string> GetTokenForCurrentApplication(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(CloudConfigurationManager.GetSetting("ida:ClientId"),
                        CloudConfigurationManager.GetSetting("ida:ClientSecret"));
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
                throw new InvalidOperationException("Failed to obtain the JWT token");

            return result.AccessToken;
        }
        public static async Task<string> GetTokenForCurrentUser()
        {
            string clientId = CloudConfigurationManager.GetSetting("ida:ClientId");
            string clientSecret = CloudConfigurationManager.GetSetting("ida:ClientSecret");
            string aadInstance = CloudConfigurationManager.GetSetting("ida:AADInstance");
            string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            string azureResourceManagerManagementUri = @"https://management.azure.com/";

            // get a token for the Graph without triggering any user interaction (from the cache, via multi-resource refresh token, etc)
            ClientCredential clientcred = new ClientCredential(clientId, clientSecret);
            // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's database
            AuthenticationContext authenticationContext = new AuthenticationContext(aadInstance + tenantID, new ADALTokenCache(signedInUserID));
            AuthenticationResult authenticationResult = authenticationContext.AcquireToken(azureResourceManagerManagementUri, clientcred);
            return authenticationResult.AccessToken;
        }
    }
}