using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Xekina.Data;
using Xekina.Data.Models;
using Newtonsoft.Json;
using Xekina.Data.Messages;
using System.Threading;

namespace XekinaEngine
{
    public class Functions
    {
        
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void ProcessQueueMessage([QueueTrigger("%RequestQueueName%")] string message, TextWriter log)
        {
            XekinaWebContext db = new XekinaWebContext();
            var incomingRequest = JsonConvert.DeserializeObject<RequestMessage>(message);
            var request = db.Requests.Find(incomingRequest.RequestID);
            Random rnd = new Random();
            
            for (int i = 0; i < 10; i++)
            {
                RequestLog rl = new RequestLog();
                rl.Request = request;
                rl.HeadlineActivity = String.Format("Activity #{0}", i);
                rl.Phase = RequestPhase.Environments;
                if (i < 7) rl.Phase = RequestPhase.DTLab;
                if (i < 5) rl.Phase = RequestPhase.BuildAndRelease;
                if (i < 3) rl.Phase = RequestPhase.VSTS;
                rl.Start = System.DateTimeOffset.Now;
                rl.Status = RequestStatus.InProgress;
                rl.Data = "you might want to put something in here";
                if (i == 10) rl.Status = RequestStatus.Completed;
                Thread.Sleep(rnd.Next(1, 5000));
                rl.Finish = System.DateTimeOffset.Now;
                db.RequestLogs.Add(rl);
                db.SaveChanges();
            }
            
            db.SaveChanges();
            log.WriteLine(message);
            db.Dispose();
        }
    }
}
