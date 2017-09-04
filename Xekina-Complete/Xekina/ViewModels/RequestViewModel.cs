using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Xekina.Data.Models;

namespace Xekina.ViewModels
{
    public class RequestViewModel
    {
        private string _formattedRequestDate="";
        private string _formattedRequestTime="";
        private DateTimeOffset _dateRequested;

        public RequestViewModel()
        {
            ResourceGroupLocationSelectList = new List<SelectListItem>();
            SubscriptionIdSelectList = new List<SelectListItem>();
        }
       
        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }
        public string SubscriptionId { get; set; }
        public List<SelectListItem> SubscriptionIdSelectList { get; set; }
        public string ResourceGroupLocation { get; set; }
        public List<SelectListItem> ResourceGroupLocationSelectList { get; set; }
        public DateTimeOffset DateRequested {
            get { return _dateRequested; }
            set
            {
                _dateRequested = value;
                _formattedRequestDate = DateRequested.LocalDateTime.ToShortDateString();
                _formattedRequestTime = DateRequested.LocalDateTime.ToShortTimeString();
            }
        }
        public string RequestedBy { get; set; }
        public RequestStatus Status { get; set; }
        public string FormattedRequestDate { get { return _formattedRequestDate; } }
        public string FormattedRequestTime { get { return _formattedRequestTime; } }

        public static explicit operator Request(RequestViewModel r)
        {
            return new Request
            {
                ProjectName = r.ProjectName,
                ProjectDescription = r.ProjectDescription,
                SubscriptionId = r.SubscriptionId,
                ResourceGroupLocation = r.ResourceGroupLocation,
                DateRequested = r.DateRequested,
                RequestedBy = r.RequestedBy,
                Status = r.Status
                
            };
        }
        public static explicit operator RequestViewModel(Request r)
        {
            return new RequestViewModel
            {
                ProjectName = r.ProjectName,
                ProjectDescription = r.ProjectDescription,
                SubscriptionId = r.SubscriptionId,
                ResourceGroupLocation = r.ResourceGroupLocation,
                DateRequested = r.DateRequested,
                RequestedBy = r.RequestedBy,
                Status = r.Status
                
            };
        }
    }
}