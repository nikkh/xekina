using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentHelper
{
    public static class AzureNameValidator
    {
        public static void ValidateVMName(string name)
        {

            if (String.IsNullOrEmpty(name))
            {
                throw new InvalidAzureNameException("VM Name cannot be null");
            }
            if ((name.Length < 1) || (name.Length > 15))
            {
                throw new InvalidAzureNameException("VM Name must be between 1 and 15 characters. Name was" + name);
            }
        } 
    }
}
