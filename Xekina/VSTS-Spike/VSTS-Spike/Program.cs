using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.Azure.KeyVault;

using Microsoft.VisualStudio.Services.Operations;
using System.Threading;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using DeploymentHelper;
using Microsoft.Azure;
using Newtonsoft.Json.Linq;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using VSTS_Spike.Models;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace VSTS_Spike
{
    class Program
    {
        const string c_collectionUri = "https://nicks-ms-subscription.visualstudio.com/DefaultCollection";
        const string c_collectionUri_release = "https://nicks-ms-subscription.vsrm.visualstudio.com/DefaultCollection";
        const string c_projectname = "xekina";
        const string c_reponame = "xekina";
        static string baseProjectName = CloudConfigurationManager.GetSetting("BaseProjectName");
        public static string GitHubPersonalAccessToken { get; set; }
        public static string VstsPersonalAccessToken { get; set; }
        static string jsonString = "";


        static ProjectHttpClient projectHttpClient = null;

        #region Utility Methods
        static VssConnection GetVssConnection()
        {
            VssCredentials creds = new VssClientCredentials();
            creds.Storage = new VssClientCredentialStorage();
            return new VssConnection(new Uri(c_collectionUri), creds);
        }
        private static void WriteRestResponseToFile(string rest, string fileName)
        {
            // This text is added only once to the file.
            if (!File.Exists(fileName))
            {
                // Create a file to write to.

                File.WriteAllText(fileName, rest);
            }
        }
        private static void Log(string logEntry, ConsoleColor colour = ConsoleColor.Gray)
        {
            Console.ForegroundColor = colour;
            Console.WriteLine(logEntry);
            Console.ForegroundColor = ConsoleColor.Gray;
            Trace.TraceInformation(logEntry);
        }
        private static string GetOutputPath(string fullName)
        {
            // This whole things is a bit rubbish...
            // Really need to parameterise the name and location (possibly branch?) of the sample project and 
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
        private static JObject CallRestApi(HttpMethod method, string projectName, string RestofUrl, bool release = false, string body = null)
        {
            string responseBody = "";
            string urlBase = CloudConfigurationManager.GetSetting("UrlBase");
            if (release) urlBase = CloudConfigurationManager.GetSetting("UrlBaseRelease");
            if ((method == HttpMethod.Post) && (body == null)) throw new Exception("CallRestApi - body cannot be null for post operations");

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                   Convert.ToBase64String(
                       System.Text.ASCIIEncoding.ASCII.GetBytes(
                           string.Format("{0}:{1}", "", VstsPersonalAccessToken))));

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
                    Log(String.Format("Api Call {0} returned {1}", requestUri, response.StatusCode));
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Error making Api call!");
                    }
                }

                return JObject.Parse(responseBody);
            }
        }
        public static async Task<string> GetToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(CloudConfigurationManager.GetSetting("ClientId"),
                        CloudConfigurationManager.GetSetting("ClientSecret"));
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
                throw new InvalidOperationException("Failed to obtain the JWT token");

            return result.AccessToken;
        }

        #endregion

        #region Json Helpers
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
        #endregion

        #region Business Logic
        private static void CreateBuildAndReleaseProcess(string projectName)
        {

            Log("Build and Release Process Creation Phase is starting", ConsoleColor.Cyan);
            Log(String.Format("Creating Build Process for project {0}", projectName));

            

            JObject buildDefinition = GetBuildDefinitionTemplate(c_collectionUri, projectName, VstsPersonalAccessToken, "ASPNetBuild");
            Log(String.Format("Sucessfully retrived build template for {0}", "ASPNetBuild"));

            buildDefinition["name"] = GetBuildNameJson(projectName);
            buildDefinition["repository"] = GetRepositoryJson(projectName);
            buildDefinition["triggers"] = GetTriggersJson();
            buildDefinition["queue"] = GetQueueJson();
            buildDefinition["description"] = "Generated by Xekina";
            Log(String.Format("Build Template = {0}", JsonConvert.SerializeObject(buildDefinition)));
            //[JSON].build.[2].inputs.codeCoverageEnabled
            string tempstr1 = buildDefinition.ToString();
            foreach (var item in buildDefinition["build"])
            {
                string s = item.ToString();
                if (item["displayName"].ToString() == "Test Assemblies")
                {
                    item["inputs"]["codeCoverageEnabled"] = "true";
                }
            }

            JObject result = CreateBuildProcess(projectName, buildDefinition, VstsPersonalAccessToken);
            string resultString = result.ToString();
            // temporarily log the output to help with the release process request...
            string path = String.Format(@"./Workfiles/BuildProcessDefinition-{0}.json", projectName);
            WriteRestResponseToFile(resultString, path);

            Log(String.Format("Build Process for project {0} created.", projectName));

            Log(String.Format("Creating Release Process for project {0}", projectName));

            // string endpoints1 = GetServiceEndpoints(projectName);


            JObject endpoint = CreateServiceEndpoint(projectName);
            jsonString = endpoint.ToString();
           
            int interval = 1;
            while (!IsServiceEndpointReady(projectName, endpoint["id"].ToString()))
            {
                Console.Write(".");
                Thread.Sleep(1000 *interval);
                interval = interval * 2;
            }
           
            
            // This is a constant within the VSTS Account
            string releaseTemplateId = "f6a07a4f-1e1f-41c0-abab-eee4b3c9117f";
            JObject releaseDefinition = GetReleaseDefinitionTemplate(c_collectionUri, projectName, VstsPersonalAccessToken, releaseTemplateId);
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

            //releaseDefinition["artifacts"][0]["definitionReference"]["artifactSourceDefinitionUrl"]["id"] = "parse";

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
            result = CreateReleaseProcess(projectName, releaseDefinition, VstsPersonalAccessToken);
            resultString = result.ToString();
            // temporarily log the output to help with the release process request...
            path = String.Format(@"./Workfiles/ReleaseProcessDefinition-{0}.json", projectName);
            WriteRestResponseToFile(resultString, path);






            Log(String.Format("Release Process for project {0} created.", projectName));
            return;
        }
        private static JObject CreateServiceEndpoint(string projectName)
        {
            JObject serviceEndpointJson = GetServiceEndpointJson();
            serviceEndpointJson["name"] = projectName + "-azure";
            serviceEndpointJson["data"]["subscriptionId"] = CloudConfigurationManager.GetSetting("SubscriptionId");
            serviceEndpointJson["authorization"]["parameters"]["tenantid"] = CloudConfigurationManager.GetSetting("TenantId");
            string body = serviceEndpointJson.ToString();
            return CallRestApi(HttpMethod.Post, projectName, "distributedtask/serviceendpoints?api-version=3.0-preview.1", false, body);
        }
        private static string GetServiceEndpoints(string projectName)
        {
            var response = CallRestApi(HttpMethod.Get, projectName, "distributedtask/serviceendpoints?api-version=3.0-preview.1");
            return response.ToString();

        }
        private static bool IsServiceEndpointReady(string projectName, string endpointId)
        {
            string urlFragment = String.Format("distributedtask/serviceendpoints/{0}?api-version=3.0-preview.1", endpointId);
            var response = CallRestApi(HttpMethod.Get, projectName, urlFragment);
            if (response["isReady"].ToString() == "True")
            {
                Log(String.Format("ServiceEndpoint {0} is ready now.", endpointId), ConsoleColor.Cyan);
                return true;
            }
             Log(String.Format("ServiceEndpoint {0} is not ready yet", endpointId));
            return false;
        }
        private static JObject CreateReleaseProcess(string projectName, JObject releaseDefinition, string VstsPersonalAccessToken)
        {
            var responseBody = "";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                   Convert.ToBase64String(
                       System.Text.ASCIIEncoding.ASCII.GetBytes(
                           string.Format("{0}:{1}", "", VstsPersonalAccessToken))));

                var requestUri = new Uri(string.Format("{0}/{1}/_apis/release/definitions?api-version=3.0-preview.1", c_collectionUri_release, projectName));
                var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                // Setup header(s)
                request.Headers.Add("Accept", "application/json");

                // Add body content
                request.Content = new StringContent(
                    releaseDefinition.ToString(),
                    Encoding.UTF8,
                    "application/json"
                );

                // Send the request
                using (HttpResponseMessage response = client.SendAsync(request).Result)
                {
                    responseBody = response.Content.ReadAsStringAsync().Result;
                    Log(String.Format("Build Process Creation status code was {0}", response.StatusCode));
                    if (!response.IsSuccessStatusCode)
                    {
                        Log(String.Format("Build Process Creation response was {0}", responseBody));
                        throw new Exception("Error creating release process!");
                    }
                }
                return JObject.Parse(responseBody);
            }
        }
        private static JObject CreateBuildProcess(string projectName, JObject buildDefinition, string VstsPersonalAccessToken)
        {
            string responseBody = null;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                   Convert.ToBase64String(
                       System.Text.ASCIIEncoding.ASCII.GetBytes(
                           string.Format("{0}:{1}", "", VstsPersonalAccessToken))));
                var requestUri = new Uri(string.Format("{0}/{1}/_apis/build/definitions?api-version=2.0", c_collectionUri, projectName));
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
                    Log(String.Format("Build Process Creation status code was {0}", response.StatusCode));
                    if (!response.IsSuccessStatusCode)
                    {
                        Log(String.Format("Build Process Creation response was {0}", responseBody));
                        throw new Exception("Error creating build process!");
                    }
                }
            }
            return JObject.Parse(responseBody);
        }
        private static JObject GetBuildDefinitionTemplate(string c_collectionUri, string projectName, string VstsPersonalAccessToken, string templateId)
        {
            string responseBody = "";
            JObject buildDefinitionTemplate = null;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                   Convert.ToBase64String(
                       System.Text.ASCIIEncoding.ASCII.GetBytes(
                           string.Format("{0}:{1}", "", VstsPersonalAccessToken))));

                var requestUri = new Uri(string.Format("{0}/{1}/_apis/build/definitions/templates/{2}?api-version=2.0", c_collectionUri, projectName, templateId));
                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                // Setup header(s)
                request.Headers.Add("Accept", "application/json");

                // Send the request
                using (HttpResponseMessage response = client.SendAsync(request).Result)
                {
                    response.EnsureSuccessStatusCode();
                    responseBody = response.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(responseBody);
                }
                buildDefinitionTemplate = GetJsonStringContents(responseBody);
            }
            return new JObject(buildDefinitionTemplate["template"].Children());
        }
        private static JObject GetReleaseDefinitionTemplate(string c_collectionUri, string projectName, string VstsPersonalAccessToken, string templateId)
        {
            string responseBody = "";
            JObject releaseDefinitionTemplate = null;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                   Convert.ToBase64String(
                       System.Text.ASCIIEncoding.ASCII.GetBytes(
                           string.Format("{0}:{1}", "", VstsPersonalAccessToken))));


                var requestUri = new Uri(string.Format("{0}/{1}/_apis/release/definitions/environmenttemplates?templateId={2}&api-version=3.0-preview.1", c_collectionUri_release, projectName, templateId));
                var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                // Setup header(s)
                request.Headers.Add("Accept", "application/json");

                // Send the request
                using (HttpResponseMessage response = client.SendAsync(request).Result)
                {
                    response.EnsureSuccessStatusCode();
                    responseBody = response.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(responseBody);
                }
                releaseDefinitionTemplate = GetJsonStringContents(responseBody);
            }
            return new JObject(releaseDefinitionTemplate);
        }
        private static void CommitSampleProject(string projectName)
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

            string url = "https://api.github.com/repos/nikkh/xekina/zipball/master";

            HttpMethod method = HttpMethod.Get;

            string body = "";

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://api.github.com/");
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                //   Convert.ToBase64String(
                //       System.Text.ASCIIEncoding.ASCII.GetBytes(
                //           string.Format("{0}:{1}", "token", CloudConfigurationManager.GetSetting("GitHubPersonalAccessToken")))));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                  Convert.ToBase64String(
                      System.Text.ASCIIEncoding.ASCII.GetBytes(
                          string.Format("{0}:{1}", "token", Program.GitHubPersonalAccessToken))));
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
                            foreach (ZipArchiveEntry entry in archive.Entries.Where(e => e.FullName.Contains("XekinaSample/XekinaWebApp") && (!String.IsNullOrEmpty(e.Name))))
                            {
                                Console.WriteLine("{0} was discovered in the archive", entry.FullName);
                                if (entry.Name.Contains(".sln"))
                                {
                                    Log("Solution " + entry.Name + "was identified");
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
                    Log(String.Format("Api Call {0} returned {1}", requestUri, response.StatusCode));

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

            Log("End of VSTS Project Creation Phase.", ConsoleColor.Cyan);
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
        #endregion

        #region Test Methods
        public static async void ReleaseSpike()
        {
            try
            {
               
                string responseBody = null;
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                            System.Text.ASCIIEncoding.ASCII.GetBytes(
                                string.Format("{0}:{1}", "", VstsPersonalAccessToken))));

                    using (HttpResponseMessage response = client.GetAsync(
                                "https://nicks-ms-subscription.vsrm.visualstudio.com/defaultcollection/nige/_apis/release/definitions/1?$expand=artifacts,environments,triggers&api-version=3.0-preview.1").Result)
                    {
                        response.EnsureSuccessStatusCode();
                        responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseBody);
                    }

                    string path = String.Format(@"./Workfiles/BuildProcessDefinition-{0}.json", "auto");
                    WriteRestResponseToFile(responseBody, path);



                }

                JObject releaseDefinitionCreateSnippet = GetJsonFileContents("./JsonSnippets/releasedefinition-create.json");
                string releaseName = string.Format("Generated Release process #{0}", DateTime.Now.Ticks);
                releaseDefinitionCreateSnippet["name"] = releaseName;
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                       Convert.ToBase64String(
                           System.Text.ASCIIEncoding.ASCII.GetBytes(
                               string.Format("{0}:{1}", "", VstsPersonalAccessToken))));

                    var requestUri = new Uri("https://nicks-ms-subscription.vsrm.visualstudio.com/defaultcollection/xekina/_apis/release/definitions?api-version=3.0-preview.1");
                    var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                    // Setup header(s)
                    request.Headers.Add("Accept", "application/json");

                    // Add body content
                    request.Content = new StringContent(
                        releaseDefinitionCreateSnippet.ToString(),
                        Encoding.UTF8,
                        "application/json"
                    );

                    // Send the request
                    using (HttpResponseMessage response = client.SendAsync(request).Result)
                    {
                        //response.EnsureSuccessStatusCode();
                        responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseBody);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public static async void BuildSpike()
        {
            try
            {
              
                string responseBody = null;
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                            System.Text.ASCIIEncoding.ASCII.GetBytes(
                                string.Format("{0}:{1}", "", VstsPersonalAccessToken))));

                    using (HttpResponseMessage response = client.GetAsync(
                                "https://nicks-ms-subscription.visualstudio.com/DefaultCollection/murhin/_apis/build/definitions/99?api-version=2.0").Result)
                    {
                        response.EnsureSuccessStatusCode();
                        responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseBody);
                    }

                    string path = String.Format(@"./Workfiles/BuildProcessDefinition-{0}.json", "auto");
                    WriteRestResponseToFile(responseBody, path);



                }

                JObject releaseDefinitionCreateSnippet = GetJsonFileContents("./JsonSnippets/releasedefinition-create.json");
                string releaseName = string.Format("Generated Release process #{0}", DateTime.Now.Ticks);
                releaseDefinitionCreateSnippet["name"] = releaseName;
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                       Convert.ToBase64String(
                           System.Text.ASCIIEncoding.ASCII.GetBytes(
                               string.Format("{0}:{1}", "", VstsPersonalAccessToken))));

                    var requestUri = new Uri("https://nicks-ms-subscription.visualstudio.com/defaultcollection/xekina/_apis/release/definitions?api-version=3.0-preview.1");
                    var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                    // Setup header(s)
                    request.Headers.Add("Accept", "application/json");

                    // Add body content
                    request.Content = new StringContent(
                        releaseDefinitionCreateSnippet.ToString(),
                        Encoding.UTF8,
                        "application/json"
                    );

                    // Send the request
                    using (HttpResponseMessage response = client.SendAsync(request).Result)
                    {
                        //response.EnsureSuccessStatusCode();
                        responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseBody);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion

        static void Main(string[] args)
        {
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetToken));
            Program.GitHubPersonalAccessToken = kv.GetSecretAsync(CloudConfigurationManager.GetSetting("GitHubPersonalAccessTokenKeyVaultUri")).Result.Value;
            Program.VstsPersonalAccessToken = kv.GetSecretAsync(CloudConfigurationManager.GetSetting("VstsPersonalAccessTokenKeyVaultUri")).Result.Value;

            //BuildSpike();
            //return;

            //ReleaseSpike();
            //return;
            Console.WriteLine();
            Log("***************************", ConsoleColor.Yellow);
            Log("** XEKINA is Starting Up **", ConsoleColor.Yellow);
            Log("***************************", ConsoleColor.Yellow);
            string projectName = null;
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
            Log("Enter your project name");
            do
            {
                projectName = Console.ReadLine();
            } while (String.IsNullOrEmpty(projectName));
            Log(string.Format("Your project will be called {0}", projectName));
           
            string ShouldCreateVSTSProject = CloudConfigurationManager.GetSetting("CreateVSTSProject");
            if (ShouldCreateVSTSProject == "YES")
            {
                CreateVSTSProject(projectName);
                string ShouldCreateBuildAndReleaseProcess = CloudConfigurationManager.GetSetting("CreateBuildAndReleaseProcess");
                if (ShouldCreateBuildAndReleaseProcess == "YES")
                {
                    CreateBuildAndReleaseProcess(projectName);
                }
             }
            
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
            string ShouldCommitSampleProject = CloudConfigurationManager.GetSetting("CommitSampleProject");
            if (ShouldCommitSampleProject == "YES")
            {
                CommitSampleProject(projectName);
            }
            Log("***************************", ConsoleColor.Yellow);
            Log("** XEKINA COMPLETED      **", ConsoleColor.Yellow);
            Log("***************************", ConsoleColor.Yellow);
            return;
        }
       
       
        
       
    }
}
