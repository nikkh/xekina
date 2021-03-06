﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace XekinaEngine
{
    public class AzureRestApi
    {
        public static async Task<JObject> Invoke(HttpMethod method, string restfulUrl, string body = null)
        {
            string token="";
            string responseBody = "";
            try
            {
                token = AuthenticationHelpers.AcquireTokenBySPN(Global.TenantId, Global.ClientId, Global.ClientSecret).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            if ((method == HttpMethod.Post) && (body == null)) throw new Exception("RestApi.Invoke - body cannot be null for post operations");
            if ((method == HttpMethod.Put) && (body == null)) throw new Exception("RestApi.Invoke - body cannot be null for put operations");
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var requestUri = new Uri(restfulUrl);
                var request = new HttpRequestMessage(method, requestUri);
                // Setup header(s)
                request.Headers.Add("Accept", "application/json");

                // Add body content
                if ((method == HttpMethod.Post) || (method == HttpMethod.Put))
                {
                    request.Content = new StringContent(
                        body,
                        Encoding.UTF8,
                        "application/json"
                    );
                }

                // Send the request
                using (HttpResponseMessage response = client.SendAsync(request).Result)
                {
                    responseBody = await response.Content.ReadAsStringAsync();
                   // WriteLog(String.Format("Api Call {0} returned {1}", requestUri, response.StatusCode));
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Error making Api call!");
                    }
                }

                return JObject.Parse(responseBody);
            }


        }
    }
}
