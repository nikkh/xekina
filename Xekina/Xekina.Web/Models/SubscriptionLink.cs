using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xekina.Web.Models
{
    public class SubscriptionLink
    {
        
        public int ID { get; set; }
        public string SubscriptionId { get; set; }
        public string SubscriptionName { get; set; }
        public bool Validated { get; set; }

    }
}

