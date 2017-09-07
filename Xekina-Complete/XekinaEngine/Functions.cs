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
        public static void ProcessCreateProjectRequest([QueueTrigger("%CreateRequestQueueName%")] string message, TextWriter log)
        {
    
            var incomingRequest = JsonConvert.DeserializeObject<RequestMessage>(message);
            TraceHelper.TraceInformation(String.Format("Processing Create request: Id is {0}", incomingRequest.RequestID.ToString()));
            TraceHelper.TraceVerbose("See WebJob log for more information");
            Engine engine = new Engine(log);
            Request request = null;
            using (XekinaWebContext context = new XekinaWebContext())
            {
                request = context.Requests.Find(incomingRequest.RequestID);
            }
            engine.CreateProjectRequestOrchestrator(request);
        }
        public static void ProcessDeleteProjectRequest([QueueTrigger("%DeleteRequestQueueName%")] string message, TextWriter log)
        {

            var incomingRequest = JsonConvert.DeserializeObject<RequestMessage>(message);
            TraceHelper.TraceInformation(String.Format("Processing Delete request: Id is {0}", incomingRequest.RequestID.ToString()));
            TraceHelper.TraceVerbose("See WebJob log for more information");
            Engine engine = new Engine(log);
            Request request = null;
            using (XekinaWebContext context = new XekinaWebContext())
            {
                request = context.Requests.Find(incomingRequest.RequestID);
            }
            if (request == null)
            {
                message = String.Format("ProcessDeleteProjectRequest: requested project not found in database {0}", incomingRequest.RequestID.ToString());
                TraceHelper.TraceError(message);
                throw new XekinaEngineProcessingException(message);
            }
            if (request.Status != RequestStatus.AwaitingDeletion)
            {
                message = String.Format("ProcessDeleteProjectRequest: requested project is not in AwaitingDeletion state {0}", incomingRequest.RequestID.ToString());
                TraceHelper.TraceError(message);
                throw new XekinaEngineProcessingException(message);
            }
            engine.DeleteProjectRequestOrchestrator(request);
        }
    }
}
