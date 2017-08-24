using System.Web.Mvc;
using Xekina.Data;

namespace Xekina.Controllers
{
    public abstract class XekinaBaseController : Controller
    {
        protected XekinaWebContext db = new XekinaWebContext();

        
    }
}