using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xekina.Models
{
    public class XekinaTemplate
    {
        public string TemplateName { get; set; }
        public string ProjectName { get; set; }
        public string ProjectStrapline { get; set; }
        public bool CreateVSTSProject { get; set; }
        public bool CreateDevTestLab { get; set; }
        public bool CreateEnvironments { get; set; }
        public bool CreateBuildAndReleaseProcess { get; set; }
        public bool CommitSampleProject { get; set; }
        public DateTimeOffset ProjectStartDate { get; set; }
        public SampleSolution SampleSolution { get; set; }

        public XekinaTemplate()
        {
            CreateVSTSProject = true;
            CreateBuildAndReleaseProcess = false;
            CreateEnvironments = false;
            CreateDevTestLab = false;
            CommitSampleProject = true;
            ProjectStartDate = DateTime.Now;
        }
    }
}
