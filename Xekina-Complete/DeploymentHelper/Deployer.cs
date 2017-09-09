// Requires the following Azure NuGet packages and related dependencies:
// package id="Microsoft.Azure.Management.Authorization" version="2.0.0"
// package id="Microsoft.Azure.Management.ResourceManager" version="1.4.0-preview"
// package id="Microsoft.Rest.ClientRuntime.Azure.Authentication" version="2.2.8-preview"

using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Rest.Azure.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentHelper
{
    /// <summary>
    /// This is a helper class for deploying an Azure Resource Manager template
    /// More info about template deployments can be found here https://go.microsoft.com/fwLink/?LinkID=733371
    /// </summary>
    public class Deployer
    {
        private TextWriter _log = null;
        private bool _local = false;
       
        
        public DeployerParameters Parameters {get; set;}

        public Deployer(DeployerParameters parameters, bool local = false, TextWriter log = null)
        {
            if (log != null) _log = log;
            if (local) _local = true;
            Parameters = parameters;
        }

        private void WriteLog(string logEntry)
        {
            _log.WriteLine(logEntry);
            if (_local) Console.WriteLine(logEntry);
            Trace.TraceInformation(logEntry);
        }
        public async Task Deploy()
        {
            // Try to obtain the service credentials
            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(Parameters.TenantId, Parameters.ClientId, Parameters.ClientSecret);

            // Read the template and parameter file contents
            JObject templateFileContents = GetJsonFileContents(Parameters.PathToTemplateFile);
            JObject parameterFileContents = GetJsonStringContents(Parameters.ParameterFileContent);


            // Create the resource manager client
            var resourceManagementClient = new ResourceManagementClient(serviceCreds);
            resourceManagementClient.SubscriptionId = Parameters.SubscriptionId;

            // Create or check that resource group exists
            EnsureResourceGroupExists(resourceManagementClient, Parameters.ResourceGroupName, Parameters.ResourceGroupLocation);

            // Start a deployment
            DeployTemplate(resourceManagementClient, Parameters.ResourceGroupName, Parameters.DeploymentName, templateFileContents, parameterFileContents);
        }

        public async void DeleteResourceGroup(string resourceGroupName)
        {
            // Create the resource manager client
            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(Parameters.TenantId, Parameters.ClientId, Parameters.ClientSecret);
           
            var resourceManagementClient = new ResourceManagementClient(serviceCreds);
            resourceManagementClient.SubscriptionId = Parameters.SubscriptionId;
            
           await resourceManagementClient.ResourceGroups.BeginDeleteAsync(resourceGroupName);
        }
        // TODO: Refactor to use Joject.Parse()
        private JObject GetJsonStringContents(string jsonString)
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
    

       

        /// <summary>
        /// Reads a JSON file from the specified path
        /// </summary>
        /// <param name="pathToJson">The full path to the JSON file</param>
        /// <returns>The JSON file contents</returns>
        private JObject GetJsonFileContents(string pathToJson)
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

        

        /// <summary>
        /// Ensures that a resource group with the specified name exists. If it does not, will attempt to create one.
        /// </summary>
        /// <param name="resourceManagementClient">The resource manager client.</param>
        /// <param name="resourceGroupName">The name of the resource group.</param>
        /// <param name="resourceGroupLocation">The resource group location. Required when creating a new resource group.</param>
        private void EnsureResourceGroupExists(ResourceManagementClient resourceManagementClient, string resourceGroupName, string resourceGroupLocation)
        {
            if (resourceManagementClient.ResourceGroups.CheckExistence(resourceGroupName) != true)
            {
                WriteLog(string.Format("Creating resource group '{0}' in location '{1}'", resourceGroupName, resourceGroupLocation));
                var resourceGroup = new ResourceGroup();
                resourceGroup.Location = resourceGroupLocation;
                resourceManagementClient.ResourceGroups.CreateOrUpdate(resourceGroupName, resourceGroup);
            }
            else
            {
                WriteLog(string.Format("Using existing resource group '{0}'", resourceGroupName));
            }
        }

        /// <summary>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 
        /// Starts a template deployment.
        /// </summary>
        /// <param name="resourceManagementClient">The resource manager client.</param>
        /// <param name="resourceGroupName">The name of the resource group.</param>
        /// <param name="deploymentName">The name of the deployment.</param>
        /// <param name="templateFileContents">The template file contents.</param>
        /// <param name="parameterFileContents">The parameter file contents.</param>
        private void DeployTemplate(ResourceManagementClient resourceManagementClient, string resourceGroupName, string deploymentName, JObject templateFileContents, JObject parameterFileContents)
        {
            WriteLog(string.Format("Starting template deployment '{0}' in resource group '{1}'", deploymentName, resourceGroupName));
            var deployment = new Deployment();

            deployment.Properties = new DeploymentProperties
            {
                Mode = DeploymentMode.Incremental,
                Template = templateFileContents,
                Parameters = parameterFileContents["parameters"].ToObject<JObject>()
            };

            var deploymentResult = resourceManagementClient.Deployments.CreateOrUpdateAsync(resourceGroupName, deploymentName, deployment);
            WriteLog(string.Format("Deployment status: {0}", deploymentResult.Status));
        }
    }
}