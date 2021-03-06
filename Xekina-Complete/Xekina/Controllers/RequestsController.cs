﻿using Microsoft.Azure;
using Microsoft.Azure.KeyVault;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Data.Entity;
using System.Net;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Web.Mvc;
using Xekina.Authentication;
using Xekina.Data;
using Xekina.Data.Messages;
using Xekina.Data.Models;
using System.Linq;
using Xekina.ViewModels;
using System.Collections.Generic;
using System.Collections;
using PagedList;

namespace Xekina.Controllers
{
    [Authorize]
    public class RequestsController : Controller
    {
        KeyVaultClient keyVaultClient;
        string queueConnectionString;
        XekinaWebContext db = new XekinaWebContext();
        public RequestsController()
        {
            keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(TokenHelper.GetTokenForCurrentApplication));
            queueConnectionString = keyVaultClient.GetSecretAsync(CloudConfigurationManager.GetSetting("QueueStorageConnectionStringKvUri")).Result.Value;
        }
        
        // GET: Requests
        public async Task<ActionResult> Index(int? page)
        {
            int pageSize;
            int.TryParse(CloudConfigurationManager.GetSetting("DefaultPageSize"), out pageSize);
            if (pageSize == 0) pageSize = 8;
            int pageNumber = (page ?? 1);
            List<Request> requests = await db.Requests.ToListAsync();
            return View(requests.ToPagedList(pageNumber, pageSize));
        }

      


        // GET: Requests/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Request request = await db.Requests.FindAsync(id);
            if (request == null)
            {
                return HttpNotFound();
            }
            return View(request);
        }

        // GET: Requests/Create
        public async Task<ActionResult> Create()
        {
            RequestViewModel vm = new RequestViewModel();
            vm.RequestedBy = User.Identity.Name;
            vm.DateRequested = System.DateTimeOffset.Now;
            
            UserDefaults userDefaults;
            using (XekinaWebContext context = new XekinaWebContext())
            {
                userDefaults = await db.UserDefaults.FindAsync(User.Identity.Name);
            }

            bool selected;
            Dictionary<String, String> locations = await new Helpers().GetResourceLocationsForUserSubscriptions();
            foreach (var location in locations)
            {
                selected = false;
                if (location.Value == userDefaults.ResourceGroupLocation) selected = true;
                vm.ResourceGroupLocationSelectList.Add(new SelectListItem { Text = location.Value, Value = location.Value, Selected=selected });
            }

            var subscriptions = await new Helpers().GetSubscriptionsForUser();
            selected = true;
            foreach (var subscription in subscriptions)
            {
                selected = false;
                if (subscription.SubscriptionId == userDefaults.DefaultSubscription) selected = true;
                vm.SubscriptionIdSelectList.Add(new SelectListItem { Text = subscription.SubscriptionName, Value = subscription.SubscriptionId, Selected = selected });
                
            }

            return View(vm);
        }

        // POST: Requests/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "ProjectName,ProjectDescription,SubscriptionId,ResourceGroupLocation,DateRequested,RequestedBy,Status")] Request request)
        {
            if (ModelState.IsValid)
            {
                db.Requests.Add(request);
                RequestLog requestLog = new RequestLog
                {
                    EventTime = request.DateRequested,
                    Request = request,
                    Status = RequestStatus.Created,
                    HeadlineActivity = "Request Created",
                     Phase = RequestPhase.Initialize, Data = request.ProjectName
                };
                db.RequestLogs.Add(requestLog);
                await db.SaveChangesAsync();
                RequestMessage rm = (RequestMessage) request;
                AddCreateRequestMessageToEngineQueue(rm);
                return RedirectToAction("Index");
            }

            return View(request);
        }

        private void AddCreateRequestMessageToEngineQueue(RequestMessage rm)
        {
            
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(queueConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            // Retrieve a reference to a queue.
            CloudQueue queue = queueClient.GetQueueReference(CloudConfigurationManager.GetSetting("CreateRequestQueueName"));

            // Create the queue if it doesn't already exist.
            queue.CreateIfNotExists();
                        
            string requestMessageString = JsonConvert.SerializeObject(rm);

            // Create a message and add it to the queue.
            CloudQueueMessage message = new CloudQueueMessage(requestMessageString);
            queue.AddMessage(message);
        }

        private void AddDeleteRequestMessageToEngineQueue(RequestMessage rm)
        {

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(queueConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            // Retrieve a reference to a queue.
            CloudQueue queue = queueClient.GetQueueReference(CloudConfigurationManager.GetSetting("DeleteRequestQueueName"));

            // Create the queue if it doesn't already exist.
            queue.CreateIfNotExists();

            string requestMessageString = JsonConvert.SerializeObject(rm);

            // Create a message and add it to the queue.
            CloudQueueMessage message = new CloudQueueMessage(requestMessageString);
            queue.AddMessage(message);
        }

        // GET: Requests/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Request request = await db.Requests.FindAsync(id);
            if (request == null)
            {
                return HttpNotFound();
            }
            return View(request);
        }

        // POST: Requests/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "RequestID,ProjectName,ProjectDescription,SubscriptionId,ResourceGroupLocation,DateRequested,RequestedBy,Status")] Request request)
        {
            if (ModelState.IsValid)
            {
                db.Entry(request).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(request);
        }

        // GET: Requests/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Request request = await db.Requests.FindAsync(id);
            if (request == null)
            {
                return HttpNotFound();
            }
            return View(request);
        }

        // POST: Requests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Request request = await db.Requests.FindAsync(id);
            request.Status=RequestStatus.AwaitingDeletion;
            await db.SaveChangesAsync();
            RequestMessage rm = (RequestMessage)request;
            AddDeleteRequestMessageToEngineQueue(rm);
            return RedirectToAction("Index");
        }

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
