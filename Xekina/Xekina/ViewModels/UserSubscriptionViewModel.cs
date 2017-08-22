using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Xekina.Data.Models;

namespace Xekina.ViewModels
{
    public class UserSubscriptionViewModel
    {
        public string SubscriptionId { get; set; }
        [Display(Name = "Subscription Name", Description = "The display name for this subscription")]
        public string SubscriptionName { get; set; }

        public string State { get; set; }

        public static explicit operator UserSubscription(UserSubscriptionViewModel usvm)
        {
            return new UserSubscription
            {
               SubscriptionId  = usvm.SubscriptionId,
               State = usvm.State,
               SubscriptionName = usvm.SubscriptionName
            };
        }

        public static explicit operator UserSubscriptionViewModel(UserSubscription us)
        {
            return new UserSubscriptionViewModel
            {
                SubscriptionId = us.SubscriptionId,
                State = us.State,
                SubscriptionName = us.SubscriptionName
            };
        }

    
    }
}