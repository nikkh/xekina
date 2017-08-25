using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xekina.Data.Models;

namespace Xekina.Data.Messages
{
    public class RequestMessage
    {

        public int RequestID { get; set; }
        
        public static explicit operator RequestMessage(Request req)
        {
            return new RequestMessage
            {
                RequestID = req.RequestID
            };
        }
    }
}

  

    

