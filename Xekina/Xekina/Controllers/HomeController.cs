using Microsoft.Azure;
using Microsoft.Azure.KeyVault;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Xekina.Authentication;
using Xekina.Data.Messages;
using Xekina.Data.Models;

namespace Xekina.Controllers
{
    [Authorize]
    public class HomeController : XekinaBaseController
    {
        public ActionResult Index()
        {
            DoMessage("Hello");
            return View();
        }

        private void DoMessage(string v)
        {
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(TokenHelper.GetTokenForCurrentApplication));
            string queueConnectionString = kv.GetSecretAsync(CloudConfigurationManager.GetSetting("QueueStorageConnectionStringKvUri")).Result.Value;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(queueConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            // Retrieve a reference to a queue.
            CloudQueue queue = queueClient.GetQueueReference(CloudConfigurationManager.GetSetting("RequestQueueName"));

            // Create the queue if it doesn't already exist.
            queue.CreateIfNotExists();

            // Get the first request to use for a message
            Request r =  db.Requests.Find(1);
            if (r == null)
            {
                throw new Exception("Didnt find a request with an Id of 1");
            }
            var requestMessge = (RequestMessage) r;
            string requestMessageString = JsonConvert.SerializeObject(requestMessge);

            // Create a message and add it to the queue.
            CloudQueueMessage message = new CloudQueueMessage(requestMessageString);
            queue.AddMessage(message);
        }

        public async Task<ActionResult> MySubscriptions()
        {
            var result = await new Helpers().GetSubscriptionsForUser();
            return View(result);
        }

       

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}