using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure;
using Microsoft.Azure.KeyVault;
using System.Diagnostics;

namespace XekinaEngine
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program
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
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            TraceHelper.TraceInformation("Xekina.Engine is starting up");
            if (Convert.ToBoolean(CloudConfigurationManager.GetSetting("UseLocalDB")))
            {
                TraceHelper.TraceInformation("Xekina.Engine will be using local database");
                string dataDirectory = CloudConfigurationManager.GetSetting("DataDirectory");
                if (String.IsNullOrEmpty(dataDirectory))
                {
                    
                    throw new Exception("Configuration requires use of LocalDB, but data directory is not set in configuration");
                }
                TraceHelper.TraceVerbose("Running locally. Data diectory will be set to " + dataDirectory);
                AppDomain.CurrentDomain.SetData("DataDirectory", dataDirectory);
            }


            
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetTokenForCurrentApplication));
            TraceHelper.TraceVerbose("Key Vault Client Created successfully");

            string queueConnectionString = kv.GetSecretAsync(CloudConfigurationManager.GetSetting("QueueStorageConnectionStringKvUri")).Result.Value;
            string dashboardConnectionString = kv.GetSecretAsync(CloudConfigurationManager.GetSetting("AzureWebJobsDashboardConnectionStringKvUri")).Result.Value;
            TraceHelper.TraceVerbose("Retrieved connection strings");

            Global.VstsPersonalAccesstoken = kv.GetSecretAsync(CloudConfigurationManager.GetSetting("VstsPersonalAccessTokenKeyVaultUri")).Result.Value;
            Global.VstsCollectionUri = CloudConfigurationManager.GetSetting("VstsCollectionUri");
            Global.GitHubPersonalAccessToken = kv.GetSecretAsync(CloudConfigurationManager.GetSetting("GitHubPersonalAccessTokenKeyVaultUri")).Result.Value;
            TraceHelper.TraceInformation("Secrets retrieved from KeyVault");

            var host = new JobHost(new JobHostConfiguration
            {
                NameResolver = new QueueNameResolver(),
                DashboardConnectionString = dashboardConnectionString,
                StorageConnectionString = queueConnectionString
            });
            // The following code ensures that the WebJob will be running continuously
            
            host.RunAndBlock();

            

        }
    }
}
