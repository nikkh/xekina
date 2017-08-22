using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xekina.Data.Models
{
    public class RequestLog
    {
        public int RequestLogID { get; set; }
        public virtual Request Request {get; set;}
        public string Data { get; set; }
    }
}
