using Microsoft.Azure;
using Microsoft.Azure.KeyVault;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Xekina.Authentication;
using Xekina.Data;
using Xekina.Data.Models;
using Xekina.ViewModels;

namespace Xekina.Controllers
{
    public class SettingsController : Controller
    {
        KeyVaultClient keyVaultClient;
        string queueConnectionString;
        XekinaWebContext db = new XekinaWebContext();
        public SettingsController()
        {
            keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(TokenHelper.GetTokenForCurrentApplication));
            queueConnectionString = keyVaultClient.GetSecretAsync(CloudConfigurationManager.GetSetting("QueueStorageConnectionStringKvUri")).Result.Value;
        }

        // GET: Settings/Edit/5
        public async Task<ActionResult> EditDefaults()
        {
            UserDefaultsViewModel vm = null;
            UserDefaults userDefaults = await db.UserDefaults.FindAsync(User.Identity.Name);
            if (userDefaults == null)
            {
                vm = new UserDefaultsViewModel(User.Identity.Name);
            }
            else
            {
                vm = (UserDefaultsViewModel)userDefaults;
            }

            var subscriptions = await new Helpers().GetSubscriptionsForUser();
            bool selected;
            foreach (var subscription in subscriptions)
            {
                selected = false;
                if (subscription.SubscriptionId == vm.DefaultSubscription) selected = true;
                vm.DefaultSubscriptionSelectList.Add(new SelectListItem { Text = subscription.SubscriptionName, Value = subscription.SubscriptionId, Selected = selected });
            }

            vm.ResourceGroupLocationSelectList = new List<SelectListItem>();
            Dictionary<String, String> locations = await new Helpers().GetResourceLocationsForUserSubscriptions();
            foreach (var location in locations)
            {
                vm.ResourceGroupLocationSelectList.Add(new SelectListItem { Text = location.Value, Value = location.Value });
            }
            return View(vm);
        }

        // POST: Settings/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditDefaults([Bind(Include = "UserId,CreateVSTSProject,CreateDevTestLab,CreateEnvironments,CreateBuildAndReleaseProcess,CommitSampleProject,ResourceGroupLocation,DefaultSubscription,GitHubPersonalAccessToken")] UserDefaultsViewModel vm)
        {
            if (ModelState.IsValid)
            {
                UserDefaults u = db.UserDefaults.Find(User.Identity.Name);
                if (u != null)
                {
                    u.CommitSampleProject = vm.CommitSampleProject;
                    u.CreateBuildAndReleaseProcess = vm.CreateBuildAndReleaseProcess;
                    u.CreateDevTestLab = vm.CreateDevTestLab;
                    u.CreateEnvironments = vm.CreateEnvironments;
                    u.CreateVSTSProject = vm.CreateVSTSProject;
                    u.ResourceGroupLocation = vm.ResourceGroupLocation;
                    u.GitHubPersonalAccessToken = vm.GitHubPersonalAccessToken;
                    u.DefaultSubscription = vm.DefaultSubscription;
                   
                }
                else
                {
                    db.UserDefaults.Add((UserDefaults) vm);
                }
                await db.SaveChangesAsync();
                return RedirectToAction("Index", "Home");
            }
            return View(vm);
        }

        // GET: Settings/Delete/5
    

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
