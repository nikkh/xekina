﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Build.WebApi;

using Microsoft.VisualStudio.Services.Operations;
using System.Threading;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using DeploymentHelper;
using Microsoft.Azure;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;
using VSTS_Spike.Models;
using System.Diagnostics;

namespace VSTS_Spike
{
    class Program
    {
        const string c_collectionUri = "https://nicks-ms-subscription.visualstudio.com/DefaultCollection";
        const string c_projectname = "xekina";
        const string c_reponame = "xekina";
        
        static ProjectHttpClient projectHttpClient = null;

        static VssConnection GetVssConnection()
        {
            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();
            return new VssConnection(new Uri(c_collectionUri), creds);
        }

        static void Main(string[] args)
        {
            Log("***************************", ConsoleColor.Yellow);
            Log("** XEKINA is Starting Up **", ConsoleColor.Yellow);
            Log("***************************", ConsoleColor.Yellow);
           
            
            
            string projectName = null;
            
            Log("Enter your project name");
            do
            {
                projectName = Console.ReadLine();
            } while (String.IsNullOrEmpty(projectName));
            Log(string.Format("Your project will be called {0}", projectName));
           

            //// Get process templates
            //ProcessHttpClient processClient = connection.GetClient<ProcessHttpClient>();
            //var processes = processClient.GetProcessesAsync().Result;

            

            string DeleteOldVSTSProjects = CloudConfigurationManager.GetSetting("DeleteOldVSTSProjects");
            if (DeleteOldVSTSProjects == "YES")
            {
                Log("Looking for previous projects you may wish to delete...", ConsoleColor.Cyan);
                // Retrieve a list of projects from the account. (and ask if they should be deleted).  This is to tidy up my test data.
                projectHttpClient = GetVssConnection().GetClient<ProjectHttpClient>();
                bool none = true;
                //then - same as above..iterate over the project references(with a hard-coded pagination of the first 10 entries only)
                foreach (var projectReference in projectHttpClient.GetProjects(top: 20, skip: 0).Result)
                {
                    var teamProject = projectHttpClient.GetProject(projectReference.Id.ToString()).Result;
                    //var urlForTeamProject = ((ReferenceLink)teamProject.Links.Links["web"]).Href;
                    
                    //Console.WriteLine("Team Project '{0}' (Id: {1}) at Web Url: '{2}' & API Url: '{3}'",
                    //teamProject.Name,
                    //teamProject.Id,
                    //urlForTeamProject,
                    //teamProject.Url);
                    if (teamProject.Description == "This is a dummy project")
                    {
                        none = false;
                        Log(string.Format("Delete project {0}? (Y/N) default=No", teamProject.Name), ConsoleColor.White);
                        string s = Console.ReadLine();

                        if (s == "Y")
                        {
                            Log(string.Format("Project {0} will be deleted!", teamProject.Name), ConsoleColor.Red);
                            projectHttpClient.QueueDeleteProject(teamProject.Id);
                            DeleteProjectResourceGroups(teamProject.Name);
                        } 
                    }
                }
                if (none) Log("I didnt find any projects that you might want to delete.");
                

            }

            string ShouldCreateVSTSProject = CloudConfigurationManager.GetSetting("CreateVSTSProject");
            if (ShouldCreateVSTSProject == "YES")
            {
                CreateVSTSProject(projectName);
            }

            BuildSpike();
            
            string ShouldCreateDevTestLab = CloudConfigurationManager.GetSetting("CreateDevTestLab");
            if (ShouldCreateDevTestLab == "YES")
            {
                CreateDevTestLab(projectName);
            }

            string ShouldCreateEnvironments = CloudConfigurationManager.GetSetting("CreateEnvironments");
            if (ShouldCreateEnvironments == "YES")
            {
                Log("Deployment Environment Creation Phase is starting", ConsoleColor.Cyan);
                CreateEnvironment(projectName, "DEV");
                CreateEnvironment(projectName, "PROD");
                Log("End of Deployment Environment Creation Phase.", ConsoleColor.Cyan);
            }
            Log("***************************", ConsoleColor.Yellow);
            Log("** XEKINA COMPLETED      **", ConsoleColor.Yellow);
            Log("***************************", ConsoleColor.Yellow);
            return;
        }

        private static void BuildSpike()
        {
            Log("Entering the build spike");
            var buildHttpClient = GetVssConnection().GetClient<BuildHttpClient>();
            //buildHttpClient.CreateDefinitionAsync(new BuildDefinition { })
        }

        private static void DeleteProjectResourceGroups(string resourceGroupName)
        {
            DeployerParameters parameters = new DeployerParameters();
            parameters.SubscriptionId = CloudConfigurationManager.GetSetting("SubscriptionId");
                        
            parameters.TenantId = CloudConfigurationManager.GetSetting("TenantId");
            parameters.ClientId = CloudConfigurationManager.GetSetting("ClientId");
            parameters.ClientSecret = CloudConfigurationManager.GetSetting("ClientSecret");
            Deployer deployer = new Deployer(parameters);
            Log(String.Format("Deleting Resource Group {0}-{1} from subscription {2}", resourceGroupName, "lab", parameters.SubscriptionId), ConsoleColor.Magenta);
            deployer.DeleteResourceGroup(String.Format("{0}-{1}", resourceGroupName, "lab"));
            Log("This will fail because the lab resource groups have locks - need to release them!");
            Log(String.Format("Deleting Resource Group {0}-{1} from subscription {2}", resourceGroupName, "dev", parameters.SubscriptionId), ConsoleColor.Magenta);
            deployer.DeleteResourceGroup(String.Format("{0}-{1}", resourceGroupName, "dev"));
            Log(String.Format("Deleting Resource Group {0}-{1} from subscription {2}", resourceGroupName, "prod", parameters.SubscriptionId), ConsoleColor.Magenta);
            deployer.DeleteResourceGroup(String.Format("{0}-{1}", resourceGroupName, "prod"));
        }

        private static void CreateVSTSProject(string projectName)
        {
            // Create a new project
            Log("VSTS Project Creation Phase is starting", ConsoleColor.Cyan);

            projectHttpClient = GetVssConnection().GetClient<ProjectHttpClient>();
            CreateProject(projectHttpClient, projectName);
            var createdProject = projectHttpClient.GetProject(projectName).Result;

            Log("Retrieve and delete default iterations from project");
            WorkHttpClient workClient = GetVssConnection().GetClient<WorkHttpClient>();
            TeamContext tc = new TeamContext(createdProject.Name);
            var iterations = workClient.GetTeamIterationsAsync(tc).Result;
            foreach (var item in iterations)
            {

                workClient.DeleteTeamIterationAsync(tc, item.Id).SyncResult();
                Log(String.Format("Deleting {0}", item.Name));
            }

            DateTime projectStartDate = new DateTime(2017, 07, 01);
            int discoveryIterationDays = 30;
            int breathingSpaceDays = 3;
            int standardIterationDays = 14;
            int alphaStandardIterations = 4;
            int privateBetaStandardIterations = 4;
            int publicBetaStandardIterations = 6;

            DateTime startDate = projectStartDate;
            DateTime endDate = startDate.AddDays(discoveryIterationDays);

            // Create a discovery iteration to get hold of a node to use as the re-assignment node when deleting stock iterations.
            Log(String.Format("Creating a Discovery Phase from {0} to {1}", startDate, endDate));
            var node = AddIteration(createdProject.Id, "Discovery", startDate, endDate);
            int discoveryNodeId = node.Id;
            TeamSettingsIteration tsi = new TeamSettingsIteration();
            tsi.Id = node.Identifier;
            var x = workClient.PostTeamIterationAsync(tsi, tc).Result;

            // Delete the standard iterations that come with a new project
            WorkItemTrackingHttpClient workItemTrackingClient = GetVssConnection().GetClient<WorkItemTrackingHttpClient>();
            workItemTrackingClient.DeleteClassificationNodeAsync(createdProject.Id, TreeStructureGroup.Iterations, "Iteration 1", discoveryNodeId).SyncResult();
            workItemTrackingClient.DeleteClassificationNodeAsync(createdProject.Id, TreeStructureGroup.Iterations, "Iteration 2", discoveryNodeId).SyncResult();
            workItemTrackingClient.DeleteClassificationNodeAsync(createdProject.Id, TreeStructureGroup.Iterations, "Iteration 3", discoveryNodeId).SyncResult();

            // Dont really need this at the moment
            // var processConfiguration = workClient.GetProcessConfigurationAsync(projectName).Result;

            // Add Alpha Phase
            startDate = endDate.AddDays(breathingSpaceDays);
            endDate = startDate.AddDays((alphaStandardIterations + 2 * standardIterationDays));
            Log(String.Format("Creating an Alpha Phase from {0} to {1}", startDate, endDate));
            node = AddIteration(createdProject.Id, "Alpha", startDate, endDate);
            tsi = new TeamSettingsIteration();
            tsi.Id = node.Identifier;
            x = workClient.PostTeamIterationAsync(tsi, tc).Result;

            endDate = startDate.AddDays(standardIterationDays);
            Log(String.Format("Creating an inception iteration from {0} to {1}", startDate, endDate));
            AddIteration(createdProject.Id, "Inception", startDate, endDate, "Alpha");
            tsi = new TeamSettingsIteration();
            tsi.Id = node.Identifier;
            x = workClient.PostTeamIterationAsync(tsi, tc).Result;

            for (int i = 0; i < alphaStandardIterations; i++)
            {
                startDate = endDate.AddDays(1);
                endDate = startDate.AddDays(standardIterationDays);
                Log(String.Format("Creating a Standard Iteration {0} from {1} to {2}", i + 1, startDate, endDate));
                AddIteration(createdProject.Id, String.Format("Iteration {0}", i + 1), startDate, endDate, "Alpha");
                tsi = new TeamSettingsIteration();
                tsi.Id = node.Identifier;
                x = workClient.PostTeamIterationAsync(tsi, tc).Result;

            }

            startDate = endDate.AddDays(1);
            endDate = startDate.AddDays(standardIterationDays);
            Log(String.Format("Creating an conclusion iteration from {0} to {1}", startDate, endDate));
            AddIteration(createdProject.Id, "Conclusion", startDate, endDate, "Alpha");
            tsi = new TeamSettingsIteration();
            tsi.Id = node.Identifier;
            x = workClient.PostTeamIterationAsync(tsi, tc).Result;

            startDate = endDate.AddDays(breathingSpaceDays);
            endDate = startDate.AddDays((privateBetaStandardIterations + publicBetaStandardIterations) * standardIterationDays);
            Log(String.Format("Creating a Beta Phase from {0} to {1}", startDate, endDate));
            AddIteration(createdProject.Id, "Beta", startDate, endDate);
            tsi = new TeamSettingsIteration();
            tsi.Id = node.Identifier;
            x = workClient.PostTeamIterationAsync(tsi, tc).Result;
            VssConnection connection = GetVssConnection();
            // test putting the sample project into GiT.
            // This would actually retrieve the files from the core template project.

            // Used later - move closer to where its needed

            GitHttpClient gitClient = connection.GetClient<GitHttpClient>();
            var repo = gitClient.GetRepositoryAsync(c_projectname, c_reponame).Result;
            gitClient = connection.GetClient<GitHttpClient>();
            repo = gitClient.GetRepositoryAsync(createdProject.Name, createdProject.Name).Result;
            GitHelper gh = new GitHelper(connection);
            var push = gh.CreatePush(createdProject.Name, repo.Name);
            var pushes = gh.ListPushesIntoMaster(createdProject.Name, repo.Name);
            Log("End of VSTS Project Creation Phase.", ConsoleColor.Cyan);
        }

        private static void CreateDevTestLab(string projectName)
        {
            Log("Dev Test Lab Creation Phase is starting", ConsoleColor.Cyan);
            Log(String.Format("Creating Lab for project {0}", projectName));

            DeployerParameters parameters = new DeployerParameters();
            parameters.SubscriptionId = CloudConfigurationManager.GetSetting("SubscriptionId");
            Log("SubscriptionId = " + parameters.SubscriptionId);
            parameters.ResourceGroupName = string.Format("{0}-{1}", projectName, "lab");
            Log("Lab resource group is " + parameters.ResourceGroupName, ConsoleColor.Magenta);
            parameters.DeploymentName = string.Format("{0}-{1}-{2}", projectName, "lab", "deployment");
            Log("Deployment will be called " + parameters.DeploymentName);
            parameters.ResourceGroupLocation = CloudConfigurationManager.GetSetting("ResourceGroupLocation");
            Log("Lab will be created in " + parameters.ResourceGroupLocation);
            parameters.PathToTemplateFile = CloudConfigurationManager.GetSetting("LabTemplateFilePath");
            parameters.PathToParameterFile = CloudConfigurationManager.GetSetting("LabTemplateParameterFilePath");
            // TODO: Get this from app.settings
            parameters.TenantId = CloudConfigurationManager.GetSetting("TenantId");
            parameters.ClientId = CloudConfigurationManager.GetSetting("ClientId");
            parameters.ClientSecret = CloudConfigurationManager.GetSetting("ClientSecret");

            string templateParameters = File.ReadAllText(parameters.PathToParameterFile);
            LabTemplateParameters labParameters = JsonConvert.DeserializeObject<LabTemplateParameters>(templateParameters);
            labParameters.parameters.newLabName.value = String.Format("{0}-{1}", projectName.ToLower(), "lab");
            labParameters.parameters.artifactRepoBranch.value = CloudConfigurationManager.GetSetting("ArtifactRepoBranch");
            labParameters.parameters.artifactRepoDisplayName.value = String.Format("{0}-{1}", projectName.ToLower(), "repo");
            labParameters.parameters.artifactRepoSecurityToken.value = CloudConfigurationManager.GetSetting("ArtifactRepoSecurityToken");
            labParameters.parameters.artifactRepoUri.value = CloudConfigurationManager.GetSetting("ArtifactRepoUri");
            labParameters.parameters.artifactRepoFolder.value = CloudConfigurationManager.GetSetting("ArtifactRepoFolder");
            labParameters.parameters.artifactRepoUri.value = CloudConfigurationManager.GetSetting("ArtifactRepoUri");
            labParameters.parameters.artifactRepoFolder.value = CloudConfigurationManager.GetSetting("ArtifactRepoFolder");
            labParameters.parameters.username.value = CloudConfigurationManager.GetSetting("LabVMUserId");
            labParameters.parameters.password.value = CloudConfigurationManager.GetSetting("LabVMPassword");
            parameters.ParameterFileContent = JsonConvert.SerializeObject(labParameters);
            Deployer deployer = new Deployer(parameters);
            deployer.Deploy().SyncResult();
            Log("End of Dev Test Lab Creation Phase.", ConsoleColor.Cyan);
        }

        private static void CreateEnvironment(string projectName, string environment)
        {
            string environmentHostingPlanSku = CloudConfigurationManager.GetSetting(string.Format("HostingPlanSkuName{0}", environment));
            DeployerParameters parameters = new DeployerParameters();
            Console.WriteLine("Creating environment " + environment);
            parameters.ResourceGroupName = String.Format("{0}-{1}", projectName, environment).ToLower();
            parameters.DeploymentName = String.Format("{0}-{1}-deployment", projectName, environment).ToLower();
            parameters.ResourceGroupLocation = CloudConfigurationManager.GetSetting("ResourceGroupLocation");
            parameters.PathToTemplateFile = CloudConfigurationManager.GetSetting("EnvTemplateFilePath");
            parameters.PathToParameterFile = CloudConfigurationManager.GetSetting("EnvTemplateParameterFilePath");
           
            parameters.TenantId = CloudConfigurationManager.GetSetting("TenantId");
            parameters.ClientId = CloudConfigurationManager.GetSetting("ClientId");
            parameters.ClientSecret = CloudConfigurationManager.GetSetting("ClientSecret");
            parameters.SubscriptionId = CloudConfigurationManager.GetSetting("SubscriptionId");

            string templateParameters = File.ReadAllText(parameters.PathToParameterFile);
            EnvironmentTemplateParameters envParameters = JsonConvert.DeserializeObject<EnvironmentTemplateParameters>(templateParameters);
            envParameters.parameters.projectName.value = projectName;
            envParameters.parameters.environmentName.value = environment;
            envParameters.parameters.skuName.value = environmentHostingPlanSku;
            envParameters.parameters.administratorLogin.value = CloudConfigurationManager.GetSetting("EnvSQLAdmin");
            envParameters.parameters.administratorLoginPassword.value = CloudConfigurationManager.GetSetting("EnvSQLAdminPassword");
            parameters.ParameterFileContent = JsonConvert.SerializeObject(envParameters);
            Deployer deployer = new Deployer(parameters);
            deployer.Deploy().SyncResult();
        }

        private static void Log(string logEntry, ConsoleColor colour = ConsoleColor.Gray)
        {
            Console.ForegroundColor = colour;
            Console.WriteLine(logEntry);
            Console.ForegroundColor = ConsoleColor.Gray;
            Trace.TraceInformation(logEntry);
        }

        private static WorkItemClassificationNode AddIteration(Guid projectId, string iterationName, DateTime startDate, DateTime endDate, string path = null)
        {
            WorkItemClassificationNode iteration = new WorkItemClassificationNode
            {
                Name = iterationName,
                StructureType = TreeNodeStructureType.Iteration
            };

            iteration.Attributes = new Dictionary<string, object>
        {
            {"startDate", startDate},
            {"finishDate", endDate}
        };

            //add the iteration to the project

            WorkItemTrackingHttpClient workItemTrackingClient = GetVssConnection().GetClient<WorkItemTrackingHttpClient>();
            var x = workItemTrackingClient.CreateOrUpdateClassificationNodeAsync(iteration, projectId, TreeStructureGroup.Iterations, path).Result;
            return x;
        }

        private static string CreateProject(ProjectHttpClient client, string projectName)
        {
            
            // We can also create new projects, i.e. like this:
            var newTeamProjectToCreate = new TeamProject();
            //var somewhatRandomValueForProjectName = projectName;

            // mandatory information is name,
            //newTeamProjectToCreate.Name = $"Project {somewhatRandomValueForProjectName}";
            newTeamProjectToCreate.Name = projectName;
            // .. description
            newTeamProjectToCreate.Description = $"This is a dummy project";

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
            var operationReference = client.QueueCreateProject(newTeamProjectToCreate).Result;

            Log(String.Format("Project '{0}' creation is '{1}'", newTeamProjectToCreate.Name, operationReference.Status));

            // tracking the status via a OperationsHttpClient (for the Project collection
            var operationsHttpClientForKnownProjectCollection = GetVssConnection().GetClient<OperationsHttpClient>();

            var projectCreationOperation = operationsHttpClientForKnownProjectCollection.GetOperation(operationReference.Id).Result;
            while (projectCreationOperation.Status != OperationStatus.Succeeded
            && projectCreationOperation.Status != OperationStatus.Failed
            && projectCreationOperation.Status != OperationStatus.Cancelled)
            {
                Console.Write(".");
                Thread.Sleep(500); // yuck

                projectCreationOperation = operationsHttpClientForKnownProjectCollection.GetOperation(operationReference.Id).Result;
            }

            // alright - creation is finished, successfully or not
            Console.WriteLine();
            Log(String.Format("Project '{0}' creation finished with State '{1}' & Message: '{2}'",
            newTeamProjectToCreate.Name,
            projectCreationOperation.Status,
            projectCreationOperation.ResultMessage ?? "n.a."));
            return newTeamProjectToCreate.Name;
        }
    }
}
