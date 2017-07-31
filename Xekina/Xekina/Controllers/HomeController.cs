using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Xekina.Authentication;

namespace Xekina.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> MySubscriptions()
        {
            TokenHelper tokenHelper = new TokenHelper();
            var token = await tokenHelper.GetTokenForCurrentUser();
            var subscriptions = await Helpers.GetSubscriptionsForUser(token);
            return View();
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