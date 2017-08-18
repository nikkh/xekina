using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using Xekina.ViewModels;

namespace Xekina.Models
{
    public class UserDefaults
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
     
        public string UserId { get; set; }
        [Display(Name = "Create VSTS project", Description = "Should xekina create a VSTS project for this request?")]
        public bool CreateVSTSProject { get; set; }
        public bool CreateDevTestLab { get; set; }
        public bool CreateEnvironments { get; set; }
        public bool CreateBuildAndReleaseProcess { get; set; }
        public bool CommitSampleProject { get; set; }
        public string ResourceGroupLocation { get; set; }
        public string ArtifactRepoUri { get; set; }
        public string ArtifactRepoFolder { get; set; }
        public string ArtifactRepoBranch { get; set; }

        public static explicit operator UserDefaultsViewModel(UserDefaults ud)
        {
            return new UserDefaultsViewModel
            {
                CommitSampleProject = ud.CommitSampleProject,
                CreateBuildAndReleaseProcess = ud.CreateBuildAndReleaseProcess,
                CreateDevTestLab = ud.CreateDevTestLab,
                CreateVSTSProject = ud.CreateVSTSProject,
                CreateEnvironments = ud.CreateEnvironments,
                ResourceGroupLocation = ud.ResourceGroupLocation,
                UserId = ud.UserId,
                ResourceGroupLocationSelectList = new List<System.Web.Mvc.SelectListItem>(),
                ArtifactRepoBranch = ud.ArtifactRepoBranch,
                ArtifactRepoFolder = ud.ArtifactRepoFolder,
                ArtifactRepoUri = ud.ArtifactRepoUri
            };
        }
    }
    
}