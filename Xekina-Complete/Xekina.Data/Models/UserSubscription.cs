using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Xekina.Data.Models
{
    public class UserSubscription
    {
        public string SubscriptionId { get; set; }
        [Display(Name = "Subscription Name", Description = "The display name for this subscription")]
        public string SubscriptionName { get; set; }

        public string State { get; set; }
    }
}