using Microsoft.Azure;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Operations;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xekina.Data.Models;
using System.IO;
using Xekina.Data;

namespace XekinaEngine
{
    public class Engine
    {
        private TextWriter log;
        XekinaWebContext context;

        public Engine(TextWriter log)
        {
            this.log = log;
            log.WriteLine("Instanciating Engine");
        }

        #region orchestration
        public void CreateProjectRequestOrchestrator(Request request)
        {
            log.WriteLine("CreateProjectRequestOrchestrator is processing the request for project {0}", request.ProjectName);
            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.Initialize, "Xekina is processing this request", request.ProjectName);
            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.VSTS, "Project Creation", String.Format("Creation of project {0} has started", request.ProjectName));
            CreateProject(request.ProjectName, request.ProjectDescription);
            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.VSTS, "Project Creation", String.Format("Creation of project {0} has finished", request.ProjectName));
            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.VSTS, "Create GIT Repository", request.ProjectName);
            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.SampleProject, "Check in sample project", request.ProjectName);
            WriteAuditRecord(request.RequestID, RequestStatus.Completed, RequestPhase.Complete, "Project created sucessfully", request.ProjectName);
        }
        #endregion

        #region business logic
        public string CreateProject(string projectName, string projectDescription)
        {
            log.WriteLine("Engine.CreateProject method invoked");
            ProjectHttpClient projectHttpClient = GetVssConnection().GetClient<ProjectHttpClient>();
            
            var newProject = new TeamProject();
            newProject.Name = projectName;
            newProject.Description = "@xekina@ " + projectDescription;
            newProject.Capabilities = new Dictionary<string, Dictionary<string, string>>
            {       
                {"versioncontrol", new Dictionary<string, string>()
                    {
                        {"sourceControlType", "Git"}
                    }
                },
                {"processTemplate", new Dictionary<string, string>()
                    {
                        // This is the Id for the GDS template, on my TFS server at least.
                        // TODO: Parameterise this template
                        {"templateTypeId", "6008e993-7062-40b0-9450-0b699b103615"} 
                    }
                }
            };

            // because project creation takes some time on the server, the creation is queued and you'll get back a 
            // ticket / reference to the operation which you can use to track the progress and/or completion
            var operationReference = projectHttpClient.QueueCreateProject(newProject).Result;
            log.WriteLine("Project '{0}' creation is '{1}'", newProject.Name, "starting");

            // tracking the status via a OperationsHttpClient (for the Project collection
            var operationsHttpClientForKnownProjectCollection = GetVssConnection().GetClient<OperationsHttpClient>();
            var projectCreationOperation = operationsHttpClientForKnownProjectCollection.GetOperation(operationReference.Id).Result;
            while (projectCreationOperation.Status != OperationStatus.Succeeded
                && projectCreationOperation.Status != OperationStatus.Failed
                && projectCreationOperation.Status != OperationStatus.Cancelled)
            {
                log.WriteLine("operation has not finished... waiting for 1 second");
                Thread.Sleep(1000); 

                projectCreationOperation = operationsHttpClientForKnownProjectCollection.GetOperation(operationReference.Id).Result;
            }
            
            log.WriteLine("Project '{0}' creation finished with State '{1}' & Message: '{2}'",
            newProject.Name,
            projectCreationOperation.Status,
            projectCreationOperation.ResultMessage ?? "none");
            return newProject.Name;
        }

        void WriteAuditRecord(int requestId, RequestStatus status, RequestPhase phase, string headlineActivity, string data)
        {
            log.WriteLine("write audit record");
            // TODO: Redesign this - start and finish on the same record?

            RequestLog requestLog = new RequestLog();
            
            requestLog.Status = status;
            requestLog.HeadlineActivity = headlineActivity;
            requestLog.Phase = phase;
            requestLog.EventTime = System.DateTimeOffset.Now;
            requestLog.Data = data;
            using (context = new XekinaWebContext())
            {
                Request request = context.Requests.Find(requestId);
                request.Status = status;
                requestLog.Request = request;
                context.RequestLogs.Add(requestLog);
                context.SaveChanges();
            }
        }

        #endregion

        #region Helper Methods

        // Get connection to project collection using personal access token.  Collection is stored in config, PAT in Key Vault.
        private static VssConnection GetVssConnection()
        {
            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();
            return new VssConnection(new Uri(Global.VstsCollectionUri), new VssBasicCredential(string.Empty, Global.VstsPersonalAccesstoken));
        }
        #endregion


        
    }
}
