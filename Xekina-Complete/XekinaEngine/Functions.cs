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
        public static void ProcessCreateProjectRequest([QueueTrigger("%RequestQueueName%")] string message, TextWriter log)
        {
    
            var incomingRequest = JsonConvert.DeserializeObject<RequestMessage>(message);
            TraceHelper.TraceInformation(String.Format("Processing request: Id is {0}", incomingRequest.RequestID.ToString()));
            TraceHelper.TraceVerbose("See WebJob log for more information");
            Engine engine = new Engine(log);
            Request request = null;
            using (XekinaWebContext context = new XekinaWebContext())
            {
                request = context.Requests.Find(incomingRequest.RequestID);
            }
            engine.CreateProjectRequestOrchestrator(request);
        }
    }
}
