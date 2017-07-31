using Microsoft.Azure;
using Microsoft.Azure.KeyVault;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Xekina.Authentication;

namespace Xekina
{
    public static class Helpers
    {
        public static void Log(string logEntry, ConsoleColor colour = ConsoleColor.Gray)
        {
            Console.ForegroundColor = colour;
            Console.WriteLine(logEntry);
            Console.ForegroundColor = ConsoleColor.Gray;
            Trace.TraceInformation(logEntry);
        }
   

    public static async Task<List<string>> GetSubscriptionsForUser(string token)
    {
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(TokenHelper.GetTokenForCurrentApplication));
            var test = kv.GetSecretAsync(CloudConfigurationManager.GetSetting("XekinaTestSecretKvUri")).Result.Value;
            List<string> subscriptions = new List<string>();
            var url = "https://management.azure.com/subscriptions?api-version=2016-06-01";
            var j = await RestApi.Invoke(HttpMethod.Get, url, token);
            var jj = j.ToString();
            foreach (var item in j["value"])
            {
                subscriptions.Add(item["subscriptionId"].ToString());
            }
            return subscriptions;
    }
    }
}