using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xekina.Data.Models
{
    public class RequestLog
    {
        public int RequestLogID { get; set; }
        public string Data { get; set; }

        public RequestPhase Phase { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset Finish { get; set; }

        public RequestStatus Status { get; set; }
        public string HeadlineActivity { get; set; }
        public String Exception { get; set;}

        public int RequestRefId { get; set; }

        [ForeignKey("RequestRefId")]
        public virtual Request Request {get; set;}
       
    }

    public enum RequestPhase { VSTS, DTLab, BuildAndRelease, Environments, SampleProject }
}
