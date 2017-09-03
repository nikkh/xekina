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

namespace XekinaEngine
{
    public class Engine
    {
        

        private static VssConnection GetVssConnection()
        {
            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();
            return new VssConnection(new Uri(Global.VstsCollectionUri), new VssBasicCredential(string.Empty, Global.VstsPersonalAccesstoken));
        }

        public string CreateProject(string projectName, string projectDescription)
        {
            ProjectHttpClient projectHttpClient = GetVssConnection().GetClient<ProjectHttpClient>();
            // We can also create new projects, i.e. like this:
            var newTeamProjectToCreate = new TeamProject();
            //var somewhatRandomValueForProjectName = projectName;
            
            // mandatory information is name,
            //newTeamProjectToCreate.Name = $"Project {somewhatRandomValueForProjectName}";
            newTeamProjectToCreate.Name = projectName;
            // .. description
            newTeamProjectToCreate.Description = "@xekina@ " + projectDescription;

            // and capabilities need to be provided
            newTeamProjectToCreate.Capabilities = new Dictionary<string, Dictionary<string, string>>
{
{
        // particularly which version control the project shall use (as of writing 'TFVC' and 'Git' are available
        "versioncontrol", new Dictionary<string, string>()
{
{"sourceControlType", "Git"}
}
},
{
        // and which Process Template to use
        "processTemplate", new Dictionary<string, string>()
{
{"templateTypeId", "6008e993-7062-40b0-9450-0b699b103615"} // This is the Id for the GDS template, on my TFS server at least.
        }
}
};

            // because project creation takes some time on the server, the creation is queued and you'll get back a 
            // ticket / reference to the operation which you can use to track the progress and/or completion
            var operationReference = projectHttpClient.QueueCreateProject(newTeamProjectToCreate).Result;

            Trace.TraceInformation(String.Format("Project '{0}' creation is '{1}'", newTeamProjectToCreate.Name, operationReference.Status));

            // tracking the status via a OperationsHttpClient (for the Project collection
            var operationsHttpClientForKnownProjectCollection = GetVssConnection().GetClient<OperationsHttpClient>();

            var projectCreationOperation = operationsHttpClientForKnownProjectCollection.GetOperation(operationReference.Id).Result;
            while (projectCreationOperation.Status != OperationStatus.Succeeded
                && projectCreationOperation.Status != OperationStatus.Failed
                && projectCreationOperation.Status != OperationStatus.Cancelled)
                {
                    Trace.WriteLine("operation has not finished... waiting 500Ms");
                    Thread.Sleep(500); // yuck

                    projectCreationOperation = operationsHttpClientForKnownProjectCollection.GetOperation(operationReference.Id).Result;
                }

            // alright - creation is finished, successfully or not
            
            Trace.TraceInformation(String.Format("Project '{0}' creation finished with State '{1}' & Message: '{2}'",
            newTeamProjectToCreate.Name,
            projectCreationOperation.Status,
            projectCreationOperation.ResultMessage ?? "n.a."));
            return newTeamProjectToCreate.Name;
        }
    }
}
