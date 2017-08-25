using Microsoft.Azure;
using Microsoft.Azure.KeyVault;
using System;
using System.Web.Mvc;
using Xekina.Authentication;
using Xekina.Data;

namespace Xekina.Controllers
{
    public abstract class XekinaBaseController : Controller
    {
        protected XekinaWebContext db = new XekinaWebContext();
        protected KeyVaultClient keyVaultClient;
        protected string queueConnectionString;
        public XekinaBaseController()
        {
            keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(TokenHelper.GetTokenForCurrentApplication));
            queueConnectionString = keyVaultClient.GetSecretAsync(CloudConfigurationManager.GetSetting("QueueStorageConnectionStringKvUri")).Result.Value;
        }
        
        


    }
}