using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Xekina
{
    public static class RedisConnectorHelper
    {
        static RedisConnectorHelper()
        {
            RedisConnectorHelper.lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                return ConnectionMultiplexer.Connect(RedisConnectionString);
            });
        }

        private static Lazy<ConnectionMultiplexer> lazyConnection;

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }

        public static string GetUserSubscriptionCacheKey(string user)
        {
            return "SUBSCRIPTIONS_" + user;
        }
        public static string RedisConnectionString { get; set; }
        public static string GetUserLocationCacheKey(string user)
        {

            return "LOCATIONS_" + user;
        }
    }
}
