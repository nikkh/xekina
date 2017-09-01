using System.Diagnostics;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace Xekina
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            Trace.TraceInformation("@@ Entering method: {0}", MethodBase.GetCurrentMethod().Name);
            
            filters.Add(new ErrorHandler.AiHandleErrorAttribute());

            
            Trace.TraceInformation("@@ Leaving method: {0}", MethodBase.GetCurrentMethod().Name);
        }
    }
}
