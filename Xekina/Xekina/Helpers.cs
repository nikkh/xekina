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
using Xekina.Models;

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
   

    public static async Task<List<UserSubscription>> GetSubscriptionsForUser(string token)
    {
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(TokenHelper.GetTokenForCurrentApplication));
            var test = kv.GetSecretAsync(CloudConfigurationManager.GetSetting("XekinaTestSecretKvUri")).Result.Value;
           
            List<UserSubscription> userSubscriptions = new List<UserSubscription>();
            var url = "https://management.azure.com/subscriptions?api-version=2016-06-01";
            var j = await RestApi.Invoke(HttpMethod.Get, url, token);
            var jj = j.ToString();
            foreach (var item in j["value"])
            {
                UserSubscription s = new UserSubscription();
                s.SubscriptionId = item["subscriptionId"].ToString();
                s.SubscriptionName = item["displayName"].ToString();
                s.State = item["state"].ToString();
                userSubscriptions.Add(s);
            }
            return userSubscriptions;
    }
    }
}