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
        public DateTimeOffset DateRequested { get; set; }
        public string RequestedBy { get; set; }
        public RequestStatus Status { get; set; }

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