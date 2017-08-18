using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Xekina.Models;

namespace Xekina.ViewModels
{
    public class UserDefaultsViewModel 
    {
        public UserDefaultsViewModel(string user)
        {
            UserId = user;
            CreateVSTSProject = true;
            CreateBuildAndReleaseProcess = true;
            CommitSampleProject = true;
            CreateDevTestLab = true;
            CreateEnvironments = true;
        }
        public UserDefaultsViewModel() { }
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
        public List<SelectListItem> ResourceGroupLocationSelectList { get; set; }

        public static explicit operator UserDefaults(UserDefaultsViewModel ud)
        {
            return new UserDefaults
            {
                CommitSampleProject = ud.CommitSampleProject,
                CreateBuildAndReleaseProcess = ud.CreateBuildAndReleaseProcess,
                CreateDevTestLab = ud.CreateDevTestLab,
                CreateVSTSProject = ud.CreateVSTSProject,
                CreateEnvironments = ud.CreateEnvironments,
                ResourceGroupLocation = ud.ResourceGroupLocation,
                UserId = ud.UserId,
                ArtifactRepoBranch = ud.ArtifactRepoBranch,
                ArtifactRepoFolder = ud.ArtifactRepoFolder,
                ArtifactRepoUri = ud.ArtifactRepoUri
            };
        }
    }
}