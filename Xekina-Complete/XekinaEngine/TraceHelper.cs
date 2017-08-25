using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XekinaEngine
{
    public class TraceHelper
    {
        public static void WriteInfo(string message, string p = null)
        {
            if (p == null)
            {
                Trace.TraceInformation(message);
            }
            else
            {
                Trace.TraceInformation(String.Format(message, p));
            }
        }
    }
}
