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
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using DeploymentHelper;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.IO.Compression;

namespace XekinaEngine
{
    public class Engine
    {
        private TextWriter log;
        XekinaWebContext context;
        bool local = false;

        public Engine(TextWriter log)
        {
            this.log = log;
            if (CloudConfigurationManager.GetSetting("UseLocalDB").ToLower() == "true") { local = true; }
            WriteLog("Instantiating Engine");
            if (local) WriteLog("Engine is running locally, log output will also be written to console");
        }

        #region orchestration
        public void CreateProjectRequestOrchestrator(Request request)
        {
            bool result = true;
            //TODO: Write a record from the Web App into audit log.
            WriteLog(String.Format("CreateProjectRequestOrchestrator is processing the request for project {0}", request.ProjectName));
            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.Initialize, "Xekina is processing this request", request.ProjectName);

            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.VSTS, "Project Creation", String.Format("Creation of project {0} has started", request.ProjectName));
            result=CreateProject(request.ProjectName, request.ProjectDescription);
            if (!result)
            {
                WriteAuditRecord(request.RequestID, RequestStatus.Error, RequestPhase.Complete, "Request aborted", request.ProjectName);
                WriteLog(String.Format("Project creation failed for project {0}", request.ProjectName));
                return;
            }
            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.VSTS, "Project Creation", String.Format("Creation of project {0} has finished", request.ProjectName));

            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.VSTS, "Project Structure", String.Format("Creation of project structure for project {0} has started", request.ProjectName));
            result = CreateProjectStructure(request.ProjectName);
            if (!result)
            {
                WriteAuditRecord(request.RequestID, RequestStatus.Error, RequestPhase.Complete, "Request aborted", request.ProjectName);
                WriteLog(String.Format("Unable to create project structure for project {0}", request.ProjectName));
                return;
            }
            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.VSTS, "Project Structure", String.Format("Creation of project structure for project {0} has finished", request.ProjectName));

            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.Environments, "Create DEV Environment", String.Format("Creation of Dev Environment for {0} has started", request.ProjectName));
            result = CreateEnvironment(request.ProjectName, "DEV", request.ResourceGroupLocation, request.SubscriptionId);
            if (!result)
            {
                WriteAuditRecord(request.RequestID, RequestStatus.Error, RequestPhase.Complete, "Request aborted", request.ProjectName);
                WriteLog(String.Format("Unable to create DEV environment for project {0}", request.ProjectName));
                return;
            }
            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.Environments, "Create DEV Environment", String.Format("Creation of Dev Environment for {0} has completed", request.ProjectName));

            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.Environments, "Create PROD Environment", String.Format("Creation of Production Environment for {0} has started", request.ProjectName));
            result = CreateEnvironment(request.ProjectName, "PROD", request.ResourceGroupLocation, request.SubscriptionId);
            if (!result)
            {
                WriteAuditRecord(request.RequestID, RequestStatus.Error, RequestPhase.Complete, "Request aborted", request.ProjectName);
                WriteLog(String.Format("Unable to create PROD environment for project {0}", request.ProjectName));
                return;
            }
            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.Environments, "Create PROD Environment", String.Format("Creation of Prod Environment for {0} has completed", request.ProjectName));

            // TODO: Only create build and release process if configuration parameters require it.
            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.VSTS, "CI/CD Processes", String.Format("Creation of build and release processes for project {0} has started", request.ProjectName));
            result = CreateBuildAndReleaseProcess(request.ProjectName, request.SubscriptionId);
            if (!result)
            {
                WriteAuditRecord(request.RequestID, RequestStatus.Error, RequestPhase.Complete, "Request aborted", request.ProjectName);
                WriteLog(String.Format("Unable to create build and release processes for project {0}", request.ProjectName));
                return;
            }
            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.VSTS, "CI/CD Processes", String.Format("Creation of build and release processes for project {0} has completed", request.ProjectName));

            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.SampleProject, "Commit sample project", String.Format("Commit sample project has started for project {0}", request.ProjectName));
            result = CommitSampleProject(request.ProjectName);
            if (!result)
            {
                WriteAuditRecord(request.RequestID, RequestStatus.Error, RequestPhase.Complete, "Request aborted", request.ProjectName);
                WriteLog(String.Format("Unable to create build and release processes for project {0}", request.ProjectName));
                return;
            }
            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.SampleProject, "Commit sample project", String.Format("Completed commit of sample project for project {0}", request.ProjectName));

            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.DTLab, "Create Dev/Test Lab", String.Format("Creating Dev/Test Lab for project {0}", request.ProjectName));
            result = CreateDevTestLab(request.ProjectName, request.ResourceGroupLocation, request.SubscriptionId);
            if (!result)
            {
                WriteAuditRecord(request.RequestID, RequestStatus.Error, RequestPhase.Complete, "Request aborted", request.ProjectName);
                WriteLog(String.Format("Unable to create dev/test lab for project {0}", request.ProjectName));
                return;
            }
            WriteAuditRecord(request.RequestID, RequestStatus.InProgress, RequestPhase.DTLab, "Create Dev/Test Lab", String.Format("Done. Dev/Test Lab created for project {0}", request.ProjectName));
            // All done! 
            WriteAuditRecord(request.RequestID, RequestStatus.Completed, RequestPhase.Complete, "Project created sucessfully", request.ProjectName);
        }
        #endregion

        #region business logic
        private bool CreateDevTestLab(string projectName, string resourceGroupLocation, string subscriptionId)
        {
            WriteLog("Dev Test Lab Creation Phase is starting");
            WriteLog(String.Format("Creating Lab for project {0}", projectName));

            DeployerParameters parameters = new DeployerParameters();
            parameters.SubscriptionId = subscriptionId;
            WriteLog("SubscriptionId = " + parameters.SubscriptionId);
            parameters.ResourceGroupName = string.Format("{0}-{1}", projectName, "lab");
            WriteLog("Lab resource group is " + parameters.ResourceGroupName);
            parameters.DeploymentName = string.Format("{0}-{1}-{2}", projectName, "lab", "deployment");
            WriteLog("Deployment will be called " + parameters.DeploymentName);
            parameters.ResourceGroupLocation = resourceGroupLocation;
            WriteLog("Lab will be created in " + parameters.ResourceGroupLocation);
            parameters.PathToTemplateFile = CloudConfigurationManager.GetSetting("LabTemplateFilePath");
            parameters.PathToParameterFile = CloudConfigurationManager.GetSetting("LabTemplateParameterFilePath");

            parameters.TenantId = CloudConfigurationManager.GetSetting("TenantId");
            parameters.ClientId = CloudConfigurationManager.GetSetting("ida:ClientId");
            parameters.ClientSecret = CloudConfigurationManager.GetSetting("ida:ClientSecret");

            string templateParameters = File.ReadAllText(parameters.PathToParameterFile);
            LabTemplateParameters labParameters = JsonConvert.DeserializeObject<LabTemplateParameters>(templateParameters);
            labParameters.parameters.newLabName.value = String.Format("{0}-{1}", projectName.ToLower(), "lab");
            labParameters.parameters.artifactRepoBranch.value = CloudConfigurationManager.GetSetting("ArtifactRepoBranch");
            labParameters.parameters.artifactRepoDisplayName.value = String.Format("{0}-{1}", projectName.ToLower(), "repo");
            labParameters.parameters.artifactRepoSecurityToken.value = Global.ArtifactRepoSecurityToken;
            labParameters.parameters.artifactRepoUri.value = CloudConfigurationManager.GetSetting("ArtifactRepoUri");
            labParameters.parameters.artifactRepoFolder.value = CloudConfigurationManager.GetSetting("ArtifactRepoFolder");
           
            labParameters.parameters.username.value = CloudConfigurationManager.GetSetting("LabVMUserId");
            labParameters.parameters.password.value = Global.DefaultLabAdminPassword;
            parameters.ParameterFileContent = JsonConvert.SerializeObject(labParameters);
            Deployer deployer = new Deployer(parameters);
            deployer.Deploy().SyncResult();
            WriteLog("End of Dev Test Lab Creation Phase.");
            return true;
        }
        private bool CommitSampleProject(string projectName)
        {
            VssConnection connection = GetVssConnection();
            GitHttpClient gitClient = connection.GetClient<GitHttpClient>();
            var repo = gitClient.GetRepositoryAsync(projectName, projectName).Result;
            //GitRef defaultBranch = gitClient.GetRefsAsync(repo.Id).Result.First();
            List<GitChange> gChanges = new List<GitChange>();
            // next, craft the branch and commit that we'll push

            GitRefUpdate newBranch = new GitRefUpdate()
            {
                Name = $"refs/heads/master",
                OldObjectId = new string('0', 40)

            };
            // TODO: Parameterize
            string url = "https://api.github.com/repos/nikkh/xekina/zipball/master";

            HttpMethod method = HttpMethod.Get;

            string body = "";

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://api.github.com/");
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                // TODO: This needs to be a user specific set of Git settings.
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                  Convert.ToBase64String(
                      System.Text.ASCIIEncoding.ASCII.GetBytes(
                          string.Format("{0}:{1}", "token", Global.GitHubPersonalAccessToken))));
                client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Mozilla", "5.0"));
                var requestUri = url; ;
                var request = new HttpRequestMessage(method, requestUri);
                // Setup header(s)
                request.Headers.Add("Accept", "application/json");

                // Add body content
                if (method == HttpMethod.Post)
                {
                    request.Content = new StringContent(
                        body,
                        Encoding.UTF8,
                        "application/json"
                    );
                }

                // Send the request
                using (HttpResponseMessage response = client.SendAsync(request).Result)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Error making Api call!");
                    }
                    using (Stream responseStream = response.Content.ReadAsStreamAsync().Result)
                    {

                        using (ZipArchive archive = new ZipArchive(responseStream, ZipArchiveMode.Read))
                        {
                            if (archive.Entries.Count == 0)
                            {
                                Console.WriteLine("There were no entries in archive!");
                            }
                            // TODO parameterise
                            foreach (ZipArchiveEntry entry in archive.Entries.Where(e => e.FullName.Contains("XekinaSample/XekinaWebApp") && (!String.IsNullOrEmpty(e.Name))))
                            {
                                Console.WriteLine("{0} was discovered in the archive", entry.FullName);
                                if (entry.Name.Contains(".sln"))
                                {
                                    WriteLog("Solution " + entry.Name + "was identified");
                                }
                                string outputPath = GetOutputPath(entry.FullName);
                                string content = null;
                                using (StreamReader reader = new StreamReader(entry.Open()))
                                {
                                    content = reader.ReadToEnd();
                                    Console.WriteLine();
                                }
                                if (entry.FullName.Contains("Views/Home/Index.cshtml"))
                                {
                                    string newContent = content.Replace("#Name_Placeholder#", projectName);
                                    content = newContent;
                                }
                                GitChange change = new GitChange()
                                {
                                    ChangeType = VersionControlChangeType.Add,
                                    Item = new GitItem() { Path = $"{outputPath}" },
                                    NewContent = new ItemContent()
                                    {
                                        ContentType = ItemContentType.RawText,
                                        Content = content
                                    }

                                };

                                gChanges.Add(change);

                            }

                            foreach (ZipArchiveEntry entry in archive.Entries.Where(e => e.FullName.Contains("XekinaSample.sln")))
                            {
                                Console.WriteLine("{0} was discovered in the archive", entry.FullName);
                                string outputPath = GetOutputPath(entry.FullName);
                                string content = null;
                                using (StreamReader reader = new StreamReader(entry.Open()))
                                {
                                    content = reader.ReadToEnd();
                                    Console.WriteLine();
                                }

                                GitChange change = new GitChange()
                                {
                                    ChangeType = VersionControlChangeType.Add,
                                    Item = new GitItem() { Path = $"{outputPath}" },
                                    NewContent = new ItemContent()
                                    {


                                        ContentType = ItemContentType.RawText,
                                        Content = content
                                    }


                                };

                                gChanges.Add(change);

                            }



                        }
                    }
                    // responseBody = response.Content.ReadAsStringAsync().Result;
                    WriteLog(String.Format("Api Call {0} returned {1}", requestUri, response.StatusCode));

                }


            }



            // Insert a readme.md
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Welcome to " + projectName + "!");
            sb.AppendLine();
            sb.AppendLine("This project was autogenerated for you by Xekina.  You should find you have:");
            sb.AppendLine();
            sb.AppendLine("* This VSTS project");
            sb.AppendLine("* A Git repository with the same name as your project");
            sb.AppendLine("* A Sample Project");
            sb.AppendLine("* A Build Process with CI");
            sb.AppendLine("* A Release Process with CD");
            sb.AppendLine("* A DevTest Lab to use for development machines");
            sb.AppendLine("* A Test and production App Service Web Apps linked to the release process");
            sb.AppendLine();
            sb.AppendFormat("[https://{0}-dev-web.azurewebsites.net](https://{0}-dev-web.azurewebsites.net)", projectName);
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendFormat("[https://{0}-prod-web.azurewebsites.net](https://{0}-prod-web.azurewebsites.net)", projectName);
            sb.AppendLine();
            GitChange readme = new GitChange()
            {
                ChangeType = VersionControlChangeType.Add,
                Item = new GitItem() { Path = $"readme.md" },
                NewContent = new ItemContent()

                {

                    Content = sb.ToString(),

                    ContentType = ItemContentType.RawText,

                },


            };

            gChanges.Add(readme);



            GitCommitRef newCommit = new GitCommitRef()
            {
                Comment = "Initial commit created by xekina",
                Changes = gChanges
            };

            // create the push with the new branch and commit
            GitPush push = gitClient.CreatePushAsync(new GitPush()
            {
                RefUpdates = new GitRefUpdate[] { newBranch },
                Commits = new GitCommitRef[] { newCommit },
            }, repo.Id).Result;
            return true;
        }
        private bool CreateEnvironment(string projectName, string environment, string resourceGroupLocation, string subscriptionId)
        {
            string environmentHostingPlanSku = CloudConfigurationManager.GetSetting(string.Format("HostingPlanSkuName{0}", environment));
            DeployerParameters parameters = new DeployerParameters();
            WriteLog("Creating environment " + environment);
            parameters.ResourceGroupName = String.Format("{0}-{1}", projectName, environment).ToLower();
            parameters.DeploymentName = String.Format("{0}-{1}-deployment", projectName, environment).ToLower();
            parameters.PathToTemplateFile = CloudConfigurationManager.GetSetting("EnvTemplateFilePath");
            parameters.PathToParameterFile = CloudConfigurationManager.GetSetting("EnvTemplateParameterFilePath");

            parameters.ResourceGroupLocation = resourceGroupLocation;
            parameters.SubscriptionId = subscriptionId;

            // TODO: Remove from App.config
            parameters.TenantId = CloudConfigurationManager.GetSetting("TenantId");
            parameters.ClientId = CloudConfigurationManager.GetSetting("ida:ClientId");
            parameters.ClientSecret = CloudConfigurationManager.GetSetting("ida:ClientSecret");
           

            string templateParameters = File.ReadAllText(parameters.PathToParameterFile);
            EnvironmentTemplateParameters envParameters = JsonConvert.DeserializeObject<EnvironmentTemplateParameters>(templateParameters);
            envParameters.parameters.projectName.value = projectName;
            envParameters.parameters.environmentName.value = environment;
            envParameters.parameters.skuName.value = environmentHostingPlanSku;
            envParameters.parameters.administratorLogin.value = CloudConfigurationManager.GetSetting("EnvSQLAdmin");
            // TODO:Move this to Key Vault
            envParameters.parameters.administratorLoginPassword.value = Global.DefaultSQLAdminPassword;
            parameters.ParameterFileContent = JsonConvert.SerializeObject(envParameters);
            Deployer deployer = new Deployer(parameters);
            deployer.Deploy().SyncResult();
            // TODO This failed with an error - maximum number of free server farms...  need to catch these.
            /* 
             "{
                  "Code": "Conflict",
                  "Message": "The maximum number of Free ServerFarms allowed in a Subscription is 10.",
                  "Target": null,
                  "Details": [
                    {
                      "Message": "The maximum number of Free ServerFarms allowed in a Subscription is 10."
                    },
                    {
                      "Code": "Conflict"
                    },
                    {
                      "ErrorEntity": {
                        "ExtendedCode": "59301",
                        "MessageTemplate": "The maximum number of {0} ServerFarms allowed in a Subscription is {1}.",
                        "Parameters": [
                          "Free",
                          "10"
                        ],
                        "Code": "Conflict",
                        "Message": "The maximum number of Free ServerFarms allowed in a Subscription is 10."
                      }
                    }
                  ],
                  "Innererror": null
                }"
             */
            return true;
        }
        private bool CreateBuildAndReleaseProcess(string projectName, string subscriptionId)
        {

            WriteLog("Build and Release Process Creation Phase is starting");
            WriteLog(String.Format("Creating Build Process for project {0}", projectName));



            JObject buildDefinition = GetBuildDefinitionTemplate(Global.VstsCollectionUri, projectName, Global.VstsPersonalAccessToken, "ASPNetBuild");
            WriteLog(String.Format("Sucessfully retrived build template for {0}", "ASPNetBuild"));

            buildDefinition["name"] = GetBuildNameJson(projectName);
            buildDefinition["repository"] = GetRepositoryJson(projectName);
            buildDefinition["triggers"] = GetTriggersJson();
            buildDefinition["queue"] = GetQueueJson();
            buildDefinition["description"] = "Generated by Xekina";
            WriteLog(String.Format("Build Template = {0}", JsonConvert.SerializeObject(buildDefinition)));
            
            string tempstr1 = buildDefinition.ToString();
            foreach (var item in buildDefinition["build"])
            {
                string s = item.ToString();
                if (item["displayName"].ToString() == "Test Assemblies")
                {
                    item["inputs"]["codeCoverageEnabled"] = "true";
                }
            }

            JObject result = CreateBuildProcess(projectName, buildDefinition);
            string resultString = result.ToString();
            // temporarily log the output to help with the release process request...
            //string path = String.Format(@"./Workfiles/BuildProcessDefinition-{0}.json", projectName);
            //WriteRestResponseToFile(resultString, path);

            WriteLog(String.Format("Build Process for project {0} created.", projectName));
            WriteLog(String.Format("Creating Release Process for project {0}", projectName));

            JObject endpoint = CreateServiceEndpoint(projectName, subscriptionId);
            //jsonString = endpoint.ToString();

            int interval = 1;
            while (!IsServiceEndpointReady(projectName, endpoint["id"].ToString()))
            {
                Console.Write(".");
                Thread.Sleep(1000 * interval);
                interval = interval * 2;
            }


            // This is a constant within the VSTS Account
            string releaseTemplateId = "f6a07a4f-1e1f-41c0-abab-eee4b3c9117f";
            JObject releaseDefinition = GetReleaseDefinitionTemplate(Global.VstsCollectionUri, projectName,  releaseTemplateId);
            releaseDefinition["name"] = GetReleaseNameJson(projectName);
            releaseDefinition["environments"] = GetReleaseEnvironmentsJson("./JsonSnippets/Release-Environment-eliot.json");
            releaseDefinition["environments"][0] = releaseDefinition["environment"];
            releaseDefinition["environments"][0]["name"] = projectName + "-dev".ToLower();
            releaseDefinition["environments"][0]["deployStep"]["tasks"][0]["inputs"]["WebAppName"] = projectName + "-dev-web".ToLower();
            releaseDefinition["environments"][0]["deployStep"]["tasks"][0]["inputs"]["ConnectedServiceName"] = endpoint["id"];
            // Need to define the build artifact that will be used for the release process
            JObject artifacts = GetJsonFileContents("./JsonSnippets/ArtifactDefinition.json");
            string artString = artifacts.ToString();
            string relString = releaseDefinition.ToString();
            releaseDefinition["artifacts"] = GetReleaseArtifactsJson("./JsonSnippets/ArtifactsDefinition.json");
            string relString2 = releaseDefinition.ToString();
            //releaseDefinition["artifacts"][0] = "Parse this from URL in build process definition";
            releaseDefinition["artifacts"][0]["type"] = "Build";
            releaseDefinition["artifacts"][0]["alias"] = result["name"];

            

            releaseDefinition["artifacts"][0]["definitionReference"]["definition"]["id"] = result["id"];
            releaseDefinition["artifacts"][0]["definitionReference"]["definition"]["name"] = result["name"];
            releaseDefinition["artifacts"][0]["definitionReference"]["project"]["id"] = result["project"]["id"];
            releaseDefinition["artifacts"][0]["definitionReference"]["project"]["name"] = result["project"]["name"];

            JToken releaseTriggerJson = GetReleaseTriggerJson();
            string test1 = releaseTriggerJson.ToString();
            releaseTriggerJson["artifactAlias"] = result["name"];
            releaseTriggerJson["triggerConditions"][0]["sourceBranch"] = "master";
            releaseDefinition.Remove("triggers");
            JArray tempest = new JArray();
            tempest.Add(releaseTriggerJson);
            releaseDefinition.Add(new JProperty("triggers", tempest));

            releaseDefinition["triggers"][0] = releaseTriggerJson;

            JObject condition = new JObject();
            condition.Add(new JProperty("name", "ReleaseStarted"));
            condition.Add(new JProperty("conditionType", "event"));
            condition.Add(new JProperty("value", ""));
            JArray conditions = new JArray(condition);
            releaseDefinition["environments"][0]["conditions"] = conditions;
            string temp = releaseDefinition.ToString();
            result = CreateReleaseProcess(projectName, releaseDefinition, Global.VstsPersonalAccessToken);
            resultString = result.ToString();
            // temporarily log the output to help with the release process request...
            //path = String.Format(@"./Workfiles/ReleaseProcessDefinition-{0}.json", projectName);
            //WriteRestResponseToFile(resultString, path);






            WriteLog(String.Format("Release Process for project {0} created.", projectName));
            return true;
        }
        private JObject CreateServiceEndpoint(string projectName, string subscriptionId)
        {
            JObject serviceEndpointJson = GetServiceEndpointJson();
            serviceEndpointJson["name"] = projectName + "-azure";
            serviceEndpointJson["data"]["subscriptionId"] = subscriptionId;
            serviceEndpointJson["authorization"]["parameters"]["tenantid"] = CloudConfigurationManager.GetSetting("TenantId");
            string body = serviceEndpointJson.ToString();
            return CallRestApi(HttpMethod.Post, projectName, "distributedtask/serviceendpoints?api-version=3.0-preview.1", false, body);
        }
        private string GetServiceEndpoints(string projectName)
        {
            var response = CallRestApi(HttpMethod.Get, projectName, "distributedtask/serviceendpoints?api-version=3.0-preview.1");
            return response.ToString();

        }
        private bool IsServiceEndpointReady(string projectName, string endpointId)
        {
            string urlFragment = String.Format("distributedtask/serviceendpoints/{0}?api-version=3.0-preview.1", endpointId);
            var response = CallRestApi(HttpMethod.Get, projectName, urlFragment);
            if (response["isReady"].ToString() == "True")
            {
                WriteLog(String.Format("ServiceEndpoint {0} is ready now.", endpointId));
                return true;
            }
            WriteLog(String.Format("ServiceEndpoint {0} is not ready yet", endpointId));
            return false;
        }

        private JObject CreateReleaseProcess(string projectName, JObject releaseDefinition, string VstsPersonalAccessToken)
        {
            var response = CallRestApi(HttpMethod.Post, projectName, "release/definitions?api-version=3.0-preview.1", true, releaseDefinition.ToString());
            WriteLog(String.Format("Release Definition {0} created successfully", releaseDefinition["name"]));
            return response;

        }
        private JObject CreateBuildProcess(string projectName, JObject buildDefinition)
        {
            string responseBody = null;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                   Convert.ToBase64String(
                       System.Text.ASCIIEncoding.ASCII.GetBytes(
                           string.Format("{0}:{1}", "", Global.VstsPersonalAccessToken))));
                var requestUri = new Uri(string.Format("{0}/{1}/_apis/build/definitions?api-version=2.0", Global.VstsCollectionUri, projectName));
                var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                // Setup header(s)
                request.Headers.Add("Accept", "application/json");

                // Add body content
                request.Content = new StringContent(
                    buildDefinition.ToString(),
                    Encoding.UTF8,
                    "application/json"
                );

                // Send the request
                using (HttpResponseMessage response = client.SendAsync(request).Result)
                {
                    responseBody = response.Content.ReadAsStringAsync().Result;
                    WriteLog(String.Format("Build Process Creation status code was {0}", response.StatusCode));
                    if (!response.IsSuccessStatusCode)
                    {
                        WriteLog(String.Format("Build Process Creation response was {0}", responseBody));
                        throw new Exception("Error creating build process!");
                    }
                }
            }
            return JObject.Parse(responseBody);
        }
        private JObject GetBuildDefinitionTemplate(string UrlBase, string projectName, string VstsPersonalAccessToken, string templateId)
        {
            string responseBody = "";
            JObject buildDefinitionTemplate = null;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                   Convert.ToBase64String(
                       System.Text.ASCIIEncoding.ASCII.GetBytes(
                           string.Format("{0}:{1}", "", Global.VstsPersonalAccessToken))));

                var requestUri = new Uri(string.Format("{0}/{1}/_apis/build/definitions/templates/{2}?api-version=2.0", Global.VstsCollectionUri, projectName, templateId));
                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                // Setup header(s)
                request.Headers.Add("Accept", "application/json");

                // Send the request
                using (HttpResponseMessage response = client.SendAsync(request).Result)
                {
                    response.EnsureSuccessStatusCode();
                    responseBody = response.Content.ReadAsStringAsync().Result;
                    WriteLog(responseBody);
                }
                buildDefinitionTemplate = GetJsonStringContents(responseBody);
            }
            return new JObject(buildDefinitionTemplate["template"].Children());
        }
        private JObject GetReleaseDefinitionTemplate(string UrlBase, string projectName, string templateId)
        {
            string responseBody = "";
            JObject releaseDefinitionTemplate = null;
            // TODO: Refactor for CallRestApi method
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                   Convert.ToBase64String(
                       System.Text.ASCIIEncoding.ASCII.GetBytes(
                           string.Format("{0}:{1}", "", Global.VstsPersonalAccessToken))));

                
                var requestUri = new Uri(string.Format("{0}/{1}/_apis/release/definitions/environmenttemplates?templateId={2}&api-version=3.0-preview.1", Global.VstsCollectionUriRelease, projectName, templateId));
                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                // Setup header(s)
                request.Headers.Add("Accept", "application/json");

                // Send the request
                using (HttpResponseMessage response = client.SendAsync(request).Result)
                {
                    response.EnsureSuccessStatusCode();
                    responseBody = response.Content.ReadAsStringAsync().Result;
                    WriteLog(responseBody);
                }
                releaseDefinitionTemplate = GetJsonStringContents(responseBody);
            }
            return new JObject(releaseDefinitionTemplate);
        }
        public bool CreateProject(string projectName, string projectDescription)
        {
            WriteLog("Engine.CreateProject method invoked");
            ProjectHttpClient projectHttpClient = GetVssConnection().GetClient<ProjectHttpClient>();
            
            var newProject = new TeamProject();
            newProject.Name = projectName;
            newProject.Description = projectDescription + " @xekina";
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
                        {"templateTypeId", Global.VstsProjectProcessTemplateId} 
                    }
                }
            };

            // because project creation takes some time on the server, the creation is queued and you'll get back a 
            // ticket / reference to the operation which you can use to track the progress and/or completion
            WriteLog(String.Format("Project '{0}' creation is '{1}'", newProject.Name, "starting"));
            OperationReference operationReference;
            try
            {
               operationReference = projectHttpClient.QueueCreateProject(newProject).Result;
            }
            catch (Exception e)
            {
               WriteLog(String.Format("Unable to create project {0}. {1}", projectName, e.InnerException.Message));
               return false;
            }
            

            // tracking the status via a OperationsHttpClient (for the Project collection
            var operationsHttpClientForKnownProjectCollection = GetVssConnection().GetClient<OperationsHttpClient>();
            var projectCreationOperation = operationsHttpClientForKnownProjectCollection.GetOperation(operationReference.Id).Result;
            while (projectCreationOperation.Status != OperationStatus.Succeeded
                && projectCreationOperation.Status != OperationStatus.Failed
                && projectCreationOperation.Status != OperationStatus.Cancelled)
            {
                WriteLog("operation has not finished... waiting for 1 second");
                Thread.Sleep(1000); 

                projectCreationOperation = operationsHttpClientForKnownProjectCollection.GetOperation(operationReference.Id).Result;
            }

            WriteLog(String.Format("Project '{0}' creation finished with State '{1}' & Message: '{2}'",
            newProject.Name,
            projectCreationOperation.Status,
            projectCreationOperation.ResultMessage ?? "none"));

            

            return true;
        }
        private bool CreateProjectStructure(string projectName)
        {
            ProjectHttpClient projectHttpClient = GetVssConnection().GetClient<ProjectHttpClient>();
            var createdProject = projectHttpClient.GetProject(projectName).Result;

            WriteLog("Retrieve and delete default iterations from project");
            WorkHttpClient workClient = GetVssConnection().GetClient<WorkHttpClient>();
            TeamContext tc = new TeamContext(createdProject.Name);
            var iterations = workClient.GetTeamIterationsAsync(tc).Result;
            foreach (var item in iterations)
            {

                workClient.DeleteTeamIterationAsync(tc, item.Id).SyncResult();
                WriteLog(String.Format("Deleting {0}", item.Name));
            }
            // TODO: Parameterise this!

            DateTime projectStartDate = DateTime.Now;
            int discoveryIterationDays = 30;
            int breathingSpaceDays = 3;
            int standardIterationDays = 14;
            int alphaStandardIterations = 4;
            int privateBetaStandardIterations = 4;
            int publicBetaStandardIterations = 6;

            DateTime startDate = projectStartDate;
            DateTime endDate = startDate.AddDays(discoveryIterationDays);

            // Create a discovery iteration to get hold of a node to use as the re-assignment node when deleting stock iterations.
            WriteLog(String.Format("Creating a Discovery Phase from {0} to {1}", startDate, endDate));
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

            
            // Add Alpha Phase
            startDate = endDate.AddDays(breathingSpaceDays);
            endDate = startDate.AddDays((alphaStandardIterations + 2 * standardIterationDays));
            WriteLog(String.Format("Creating an Alpha Phase from {0} to {1}", startDate, endDate));
            node = AddIteration(createdProject.Id, "Alpha", startDate, endDate);
            tsi = new TeamSettingsIteration();
            tsi.Id = node.Identifier;
            x = workClient.PostTeamIterationAsync(tsi, tc).Result;

            endDate = startDate.AddDays(standardIterationDays);
            WriteLog(String.Format("Creating an inception iteration from {0} to {1}", startDate, endDate));
            AddIteration(createdProject.Id, "Inception", startDate, endDate, "Alpha");
            tsi = new TeamSettingsIteration();
            tsi.Id = node.Identifier;
            x = workClient.PostTeamIterationAsync(tsi, tc).Result;

            for (int i = 0; i < alphaStandardIterations; i++)
            {
                startDate = endDate.AddDays(1);
                endDate = startDate.AddDays(standardIterationDays);
                WriteLog(String.Format("Creating a Standard Iteration {0} from {1} to {2}", i + 1, startDate, endDate));
                AddIteration(createdProject.Id, String.Format("Iteration {0}", i + 1), startDate, endDate, "Alpha");
                tsi = new TeamSettingsIteration();
                tsi.Id = node.Identifier;
                x = workClient.PostTeamIterationAsync(tsi, tc).Result;

            }

            startDate = endDate.AddDays(1);
            endDate = startDate.AddDays(standardIterationDays);
            WriteLog(String.Format("Creating an conclusion iteration from {0} to {1}", startDate, endDate));
            AddIteration(createdProject.Id, "Conclusion", startDate, endDate, "Alpha");
            tsi = new TeamSettingsIteration();
            tsi.Id = node.Identifier;
            x = workClient.PostTeamIterationAsync(tsi, tc).Result;

            startDate = endDate.AddDays(breathingSpaceDays);
            endDate = startDate.AddDays((privateBetaStandardIterations + publicBetaStandardIterations) * standardIterationDays);
            WriteLog(String.Format("Creating a Beta Phase from {0} to {1}", startDate, endDate));
            AddIteration(createdProject.Id, "Beta", startDate, endDate);
            tsi = new TeamSettingsIteration();
            tsi.Id = node.Identifier;
            x = workClient.PostTeamIterationAsync(tsi, tc).Result;
            //TODO: Complete project iteration creation
            return true;
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
        void WriteAuditRecord(int requestId, RequestStatus status, RequestPhase phase, string headlineActivity, string data)
        {
            WriteLog("write audit record");
            
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
            return new VssConnection(new Uri(Global.VstsCollectionUri), new VssBasicCredential(string.Empty, Global.VstsPersonalAccessToken));
        }
        private static string GetOutputPath(string fullName)
        {
            
            // TODO: parameterise the name and location (possibly branch?) of the sample project and 
            // then read the contents from the .csproj file?
            string outputPath = null;
            if (fullName.Contains("XekinaSample.sln"))
            {
                outputPath = "XekinaSample.sln";
            }
            else
            {
                var startIndex = fullName.IndexOf("XekinaWebApp");
                int length = fullName.Length - startIndex + 1;
                outputPath = fullName.Substring(startIndex);
            }
            return outputPath;
        }
        // TODO: If needed, you can do this with JObject.Parse()...
        private static JObject GetJsonStringContents(string jsonString)
        {
            JObject json = new JObject();
            using (StringReader file = new StringReader(jsonString.ToString()))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    json = (JObject)JToken.ReadFrom(reader);
                    return json;
                }
            }
        }

        private void WriteLog(string logEntry)
        {
            log.WriteLine(logEntry);
            if (local) Console.WriteLine(logEntry);
        }
        private JObject CallRestApi(HttpMethod method, string projectName, string RestofUrl, bool release = false, string body = null)
        {
            string responseBody = "";
            string urlBase = Global.VstsCollectionUri;
            if (release) urlBase = Global.VstsCollectionUriRelease;
            if ((method == HttpMethod.Post) && (body == null)) throw new Exception("CallRestApi - body cannot be null for post operations");

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                   Convert.ToBase64String(
                       System.Text.ASCIIEncoding.ASCII.GetBytes(
                           string.Format("{0}:{1}", "", Global.VstsPersonalAccessToken))));

                var requestUri = new Uri(string.Format("{0}/{1}/_apis/{2}", urlBase, projectName, RestofUrl));
                var request = new HttpRequestMessage(method, requestUri);
                // Setup header(s)
                request.Headers.Add("Accept", "application/json");

                // Add body content
                if (method == HttpMethod.Post)
                {
                    request.Content = new StringContent(
                        body,
                        Encoding.UTF8,
                        "application/json"
                    );
                }

                // Send the request
                using (HttpResponseMessage response = client.SendAsync(request).Result)
                {
                    responseBody = response.Content.ReadAsStringAsync().Result;
                    WriteLog(String.Format("Api Call {0} returned {1}", requestUri, response.StatusCode));
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Error making Api call!");
                    }
                }

                return JObject.Parse(responseBody);
            }
        }
        #endregion

        private static JToken GetReleaseEnvironmentsJson(string snippetPath)
        {
            return GetJsonFileContents("./JsonSnippets/Release-Environment-eliot.json")["environments"];
        }
        private static JToken GetReleaseArtifactsJson(string snippetPath)
        {
            return GetJsonFileContents("./JsonSnippets/ArtifactDefinition.json")["artifacts"];
        }
        private static JToken GetBuildNameJson(string projectName)
        {
            DateTime d = DateTime.Now;
            return string.Format("Build Process ({0}) #{1}{2}{3}.{4}{5}{6}", projectName, d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second);
        }
        private static JToken GetReleaseNameJson(string projectName)
        {
            DateTime d = DateTime.Now;
            return string.Format("Release Process ({0}) #{1}{2}{3}.{4}{5}{6}", projectName, d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second);
        }
        private static JToken GetQueueJson()
        {
            return new JObject(
             new JProperty("Pool",
                new JObject(
                    new JProperty("Name", "Hosted"),
                    new JProperty("IsHosted", "True"))),
            new JProperty("Name", "Hosted"));
        }
        private static JToken GetRepositoryJson(string projectName)
        {
            return new JObject(
                new JProperty("Properties", new JObject(
                new JProperty("cleanOptions", "0"),
                new JProperty("labelSources", "0"),
                new JProperty("labelSourcesFormat", "$(build.buildNumber)"),
                new JProperty("reportBuildStatus", "true"),
                new JProperty("gitLfsSupport", "false"),
                new JProperty("skipSyncSource", "false"),
                new JProperty("checkoutNestedSubmodules", "false"),
                new JProperty("fetchDepth", "0"))),
             new JProperty("Type", "TfsGit"),
             new JProperty("Name", projectName),
             new JProperty("DefaultBranch", "refs/heads/master"),
             new JProperty("Clean", "false"),
             new JProperty("CheckoutSubmodules", "false"));
        }
        private static JObject GetServiceEndpointJson()
        {
            JObject j = new JObject(
                     new JProperty("name", "<insert name here>"),
                     new JProperty("data", new JObject(
                         new JProperty("subscriptionId", "<insert subscriptionid here>"),
                         new JProperty("subscriptionName", "Nicks Internal Subscription"),
                         new JProperty("creationMode", "Automatic")
                         )),
                     new JProperty("type", "azurerm"),
                     new JProperty("url", "https://management.azure.com/"),
                     new JProperty("authorization", new JObject(
                     new JProperty("scheme", "ServicePrincipal"),
                     new JProperty("parameters", new JObject(
                     new JProperty("tenantid", "<insert tenantid here"))))));
            return j;
        }
        private static JObject GetReleaseTriggerJson()
        {
            JObject j = new JObject();
            j.Add(new JProperty("artifactAlias", "<Replace with build process name>"));
            j.Add(new JProperty("triggerType", "artifactSource"));

            JProperty sourceBranch = new JProperty("sourceBranch", "<Replace with source branch>");
            JProperty tags = new JProperty("tags", new JArray());
            JObject triggerConditionObject = new JObject();
            triggerConditionObject.Add(sourceBranch);
            triggerConditionObject.Add(tags);
            JArray triggerConditionObjects = new JArray();
            triggerConditionObjects.Add(triggerConditionObject);


            JProperty triggerConditions = new JProperty("triggerConditions", triggerConditionObjects);
            j.Add(triggerConditions);
            return j;

        }
        private static JToken GetTriggersJson()
        {
            return new JArray(
                         new JObject(
                             new JProperty("branchFilters", new JArray("+refs/heads/master")),
                             new JProperty("pathFilters"),
                             new JProperty("batchChanges", false),
                             new JProperty("maxConcurrentBuildsPerBranch", 1),
                             new JProperty("pollingInterval", 0),
                             new JProperty("triggerType", "continuousIntegration")));
        }
        private static JObject GetJsonFileContents(string pathToJson)
        {
            JObject templatefileContent = new JObject();
            using (StreamReader file = File.OpenText(pathToJson))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    templatefileContent = (JObject)JToken.ReadFrom(reader);
                    return templatefileContent;
                }
            }
        }
    }
}
