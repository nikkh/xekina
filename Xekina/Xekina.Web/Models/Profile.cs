using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xekina.Web.Models
{
    public class Profile
    {
        
        public int ID { get; set; }
        public string TestDataItem { get; set; }

        public virtual List<SubscriptionLink> Subscriptions { get; set; }
    }

  
}
