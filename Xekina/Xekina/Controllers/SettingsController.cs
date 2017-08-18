using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Xekina.DataAccess;
using Xekina.Models;
using Xekina.ViewModels;
using Xekina.Authentication;
using System.Configuration;
using StackExchange.Redis;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure;

namespace Xekina.Controllers
{
    public class SettingsController : XekinaBaseController
    {
       

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
        public async Task<ActionResult> EditDefaults([Bind(Include = "UserId,CreateVSTSProject,CreateDevTestLab,CreateEnvironments,CreateBuildAndReleaseProcess,CommitSampleProject,ResourceGroupLocation,GitHubPersonalAccessToken")] UserDefaultsViewModel vm)
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
