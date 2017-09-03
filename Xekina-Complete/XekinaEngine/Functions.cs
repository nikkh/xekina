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
            Engine engine = new Engine();

            var incomingRequest = JsonConvert.DeserializeObject<RequestMessage>(message);

            TraceHelper.TraceInformation(String.Format("Incoming message: {0}", incomingRequest.RequestID.ToString()));
            //log.WriteLine("Incoming Message - Request Id" + incomingRequest.RequestID.ToString());

            var request = db.Requests.Find(incomingRequest.RequestID);

            WriteLogRecord(db, request, RequestStatus.InProgress, RequestPhase.Initialize, "Xekina is processing this request", request.ProjectName);
            WriteLogRecord(db, request, RequestStatus.InProgress, RequestPhase.VSTS, "Build of VSTS Project", String.Format("Creation of project {0} has started", request.ProjectName));
            engine.CreateProject(request.ProjectName, request.ProjectDescription);
            WriteLogRecord(db, request, RequestStatus.InProgress, RequestPhase.VSTS, "Build of VSTS Project", String.Format("Creation of project {0} has finished", request.ProjectName));
            WriteLogRecord(db, request, RequestStatus.InProgress, RequestPhase.VSTS, "Create GIT Repository", request.ProjectName);
            WriteLogRecord(db, request, RequestStatus.InProgress, RequestPhase.SampleProject, "Check in sample project", request.ProjectName);
            WriteLogRecord(db, request, RequestStatus.Completed, RequestPhase.Complete, "Project created sucessfully", request.ProjectName);




            db.Dispose();
        }

        static void WriteLogRecord(XekinaWebContext db, Request request, RequestStatus status, RequestPhase phase, string headlineActivity, string data)
        {
            Random rnd = new Random();
            RequestLog requestLog = new RequestLog();
            request.Status = status;
            requestLog.Status = status;
            requestLog.HeadlineActivity = headlineActivity;
            requestLog.Request = request;
            requestLog.Phase = phase;
            requestLog.Start = System.DateTimeOffset.Now;
            Thread.Sleep(rnd.Next(1, 5000));
            requestLog.Finish = System.DateTimeOffset.Now;
            requestLog.Data = request.ProjectName;
            db.RequestLogs.Add(requestLog);
            db.SaveChanges();

        }
    }
}
