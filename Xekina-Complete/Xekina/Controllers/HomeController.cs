using Microsoft.Azure;
using Microsoft.Azure.KeyVault;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
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
           Trace.TraceInformation("@@ Home:Index()");
           return View();
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