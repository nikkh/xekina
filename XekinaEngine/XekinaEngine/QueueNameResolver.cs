using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XekinaEngine
{
    public class QueueNameResolver : INameResolver
    {
        public string Resolve(string name)
        {
            return CloudConfigurationManager.GetSetting(name);
        }
    }
}
