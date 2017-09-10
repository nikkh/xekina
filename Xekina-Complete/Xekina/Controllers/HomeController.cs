using Microsoft.Azure;
using Microsoft.Azure.KeyVault;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Xekina.Authentication;
using Xekina.Authorisation;
using Xekina.Data.Messages;
using Xekina.Data.Models;

namespace Xekina.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        KeyVaultClient keyVaultClient;
        string queueConnectionString;

        public HomeController()
        {
            keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(TokenHelper.GetTokenForCurrentApplication));
            queueConnectionString = keyVaultClient.GetSecretAsync(CloudConfigurationManager.GetSetting("QueueStorageConnectionStringKvUri")).Result.Value;
        }

        public ActionResult Index()
        {
           Trace.TraceInformation("@@ Home:Index()");
           return View();
        }

      

        public async Task<ActionResult> MySubscriptions()
        {
            var result = await new Helpers().GetSubscriptionsForUser();
            return View(result);
        }

        public ActionResult FileUpload()
        {
            return View();
        }

        [HttpPost]
        public ActionResult FileUpload(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
                try
                {
                    Xekina.Helpers.SaveFileToBlobStorage(queueConnectionString, file);
                    ViewBag.Message = "File uploaded successfully";
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "ERROR:" + ex.Message.ToString();
                }
            else
            {
                ViewBag.Message = "You have not specified a file.";
            }
            return View();
        }

        public async Task<ActionResult> About()
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