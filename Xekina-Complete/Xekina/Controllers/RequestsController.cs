using Microsoft.Azure;
using Microsoft.Azure.KeyVault;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Data.Entity;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using Xekina.Authentication;
using Xekina.Data;
using Xekina.Data.Messages;
using Xekina.Data.Models;

namespace Xekina.Controllers
{
    public class RequestsController : XekinaBaseController
    {
        
        // GET: Requests
        public async Task<ActionResult> Index()
        {
            return View(await db.Requests.ToListAsync());
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
        public ActionResult Create()
        {
            return View();
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
                await db.SaveChangesAsync();
                RequestMessage rm = (RequestMessage) request;
                AddRequestMessageToEngineQueue(rm);
                return RedirectToAction("Index");
            }

            return View(request);
        }

        private void AddRequestMessageToEngineQueue(RequestMessage rm)
        {
            
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(queueConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            // Retrieve a reference to a queue.
            CloudQueue queue = queueClient.GetQueueReference(CloudConfigurationManager.GetSetting("RequestQueueName"));

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
            db.Requests.Remove(request);
            await db.SaveChangesAsync();
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
