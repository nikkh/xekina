using Microsoft.Azure;
using Microsoft.Azure.KeyVault;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Xekina.Authentication;
using Xekina.Models;

namespace Xekina
{
    public class Helpers
    {
       

        public static void Log(string logEntry, ConsoleColor colour = ConsoleColor.Gray)
        {
            Console.ForegroundColor = colour;
            Console.WriteLine(logEntry);
            Console.ForegroundColor = ConsoleColor.Gray;
            Trace.TraceInformation(logEntry);
        }



        public async Task<Dictionary<string, string>> GetResourceLocationsForUserSubscriptions()
        {
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            string user = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value;

            List<UserSubscription> userSubscriptions;
            string userSubscriptionCacheKey = RedisConnectorHelper.GetUserSubscriptionCacheKey(user);
            string serializedSubscriptions = cache.StringGet(userSubscriptionCacheKey);
            if (!String.IsNullOrEmpty(serializedSubscriptions))
            {
                userSubscriptions = JsonConvert.DeserializeObject<List<UserSubscription>>(serializedSubscriptions);
            }
            else
            {
                userSubscriptions = await GetSubscriptionsForUser();
            }

            string userLocationCacheKey = RedisConnectorHelper.GetUserLocationCacheKey(user);
            Dictionary<string, string> consolidatedLocations = new Dictionary<string, string>();
            string serializedLocations = cache.StringGet(userLocationCacheKey);
            if (!String.IsNullOrEmpty(serializedLocations))
            {
                consolidatedLocations = JsonConvert.DeserializeObject<Dictionary<string, string>>(serializedLocations);
            }
            else
            {
                foreach (var subscription in userSubscriptions)
                {
                    var url = String.Format("https://management.azure.com/subscriptions/{0}/locations?api-version=2016-06-01", subscription.SubscriptionId);
                    var j = await RestApi.Invoke(HttpMethod.Get, url);
                    var jj = j.ToString();
                    foreach (var item in j["value"])
                    {
                        string location = item["displayName"].ToString();
                        if (!consolidatedLocations.ContainsKey(location))
                        {
                            consolidatedLocations.Add(location, location);
                        }
                   }
               }
                cache.StringSet(userLocationCacheKey, JsonConvert.SerializeObject(serializedLocations));
            }
            return consolidatedLocations;
        }


    public async Task<List<UserSubscription>> GetSubscriptionsForUser()
    {
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            string user = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value;
            

            string userSubscriptionCacheKey = RedisConnectorHelper.GetUserSubscriptionCacheKey(user);
            List<UserSubscription> userSubscriptions = new List<UserSubscription>(); 
            string serializedSubscriptions = cache.StringGet(userSubscriptionCacheKey);
            if (!String.IsNullOrEmpty(serializedSubscriptions))
            {
                userSubscriptions = JsonConvert.DeserializeObject<List<UserSubscription>>(serializedSubscriptions);
            }
            else
            {
                var url = "https://management.azure.com/subscriptions?api-version=2016-06-01";
                var j = await RestApi.Invoke(HttpMethod.Get, url);
                var jj = j.ToString();
                foreach (var item in j["value"])
                {
                    UserSubscription s = new UserSubscription();
                    s.SubscriptionId = item["subscriptionId"].ToString();
                    s.SubscriptionName = item["displayName"].ToString();
                    s.State = item["state"].ToString();
                    userSubscriptions.Add(s);
                }
                cache.StringSet(userSubscriptionCacheKey, JsonConvert.SerializeObject(userSubscriptions));
            }

            return userSubscriptions;
    }
    }
}