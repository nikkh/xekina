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
using System.Diagnostics;
using System.Reflection;

namespace Xekina.Authentication
{
    public static class TokenHelper
    {
        public static async Task<string> GetTokenForCurrentApplication(string authority, string resource, string scope)
        {
            Trace.TraceInformation("@@ Entering method: {0}", MethodBase.GetCurrentMethod().Name);
            
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(CloudConfigurationManager.GetSetting("ida:ClientId"),
                        CloudConfigurationManager.GetSetting("ida:ClientSecret"));
            AuthenticationResult result = authContext.AcquireTokenAsync(resource, clientCred).Result;

            if (result == null)
                throw new InvalidOperationException("Failed to obtain the JWT token for application");
            
            Trace.TraceInformation("@@ Leaving method: {0}", MethodBase.GetCurrentMethod().Name);
            return result.AccessToken;
        }
        public static async Task<string> GetTokenForCurrentUser()
        {
            Trace.TraceInformation("@@ Entering method: {0}", MethodBase.GetCurrentMethod().Name);
            
            string azureResourceManagerManagementUri = @"https://management.azure.com/";
            return await GetTokenForCurrentUser(azureResourceManagerManagementUri);
        }

        public static async Task<string> GetTokenForCurrentUser(string resourceURI)
        {
            Trace.TraceInformation("@@ Entering method: {0}", MethodBase.GetCurrentMethod().Name);
            Trace.TraceInformation("@@ Specific resource Uri requested: {0}", resourceURI);

            string clientId = CloudConfigurationManager.GetSetting("ida:ClientId");
            string clientSecret = CloudConfigurationManager.GetSetting("ida:ClientSecret");
            string aadInstance = CloudConfigurationManager.GetSetting("ida:AADInstance");
            string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            

            // get a token for the Graph without triggering any user interaction (from the cache, via multi-resource refresh token, etc)
            ClientCredential clientcred = new ClientCredential(clientId, clientSecret);
            // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's database
            AuthenticationContext authenticationContext = new AuthenticationContext(aadInstance + tenantID, new ADALTokenCache(signedInUserID));
            AuthenticationResult result = authenticationContext.AcquireTokenAsync(resourceURI, clientcred).Result;
            if (result == null)
                throw new InvalidOperationException("Failed to obtain the JWT token for user");

            Trace.TraceInformation("@@ Leaving method: {0}", MethodBase.GetCurrentMethod().Name);
            return result.AccessToken;
        }
    }
}