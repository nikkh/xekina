using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Xekina.Models
{
    public class Request
    {
        public int ID { get; set; }
        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }
        public string SubscriptionId { get; set; }
        public string ResourceGroupLocation { get; set; }

        public DateTimeOffset DateRequested { get; set; }
        public string RequestedBy { get; set; }

        public RequestStatus Status { get; set; }

    }

    public enum RequestStatus { Created, InProgress, Completed, Error }
}