using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XekinaEngine
{
    public static class Global
    {
        public static string VstsPersonalAccessToken { get; set; }
        public static string VstsCollectionUri { get; set; }
        public static string GitHubPersonalAccessToken { get; set; }
        public static string VstsProjectProcessTemplateId { get; set; }
        public static string VstsCollectionUriRelease { get; set; }
        public static string DefaultSQLAdminPassword { get; set; }
        public static string DefaultLabAdminPassword { get; set; }
        public static string ArtifactRepoSecurityToken { get; set; }
    }
}
