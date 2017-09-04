using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XekinaEngine
{
    public class ProjectName
    {
        public string value { get; set; }
    }

    public class EnvironmentName
    {
        public string value { get; set; }
    }

    public class AdministratorLogin
    {
        public string value { get; set; }
    }

    public class AdministratorLoginPassword
    {
        public string value { get; set; }
    }

    public class DatabaseName
    {
        public string value { get; set; }
    }

    public class SkuName
    {
        public string value { get; set; }
    }

    public class EnvironmentParameters
    {
        public ProjectName projectName { get; set; }
        public EnvironmentName environmentName { get; set; }
        public SkuName skuName { get; set; }
        public AdministratorLogin administratorLogin { get; set; }
        public AdministratorLoginPassword administratorLoginPassword { get; set; }
        public DatabaseName databaseName { get; set; }
    }

    public class EnvironmentTemplateParameters
    {
        public string schema { get; set; }
        public string contentVersion { get; set; }
    public EnvironmentParameters parameters { get; set; }
}
}
