using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Signum.Web.ScriptCombiner
{
    public class CombineController : Controller
    {
        [AcceptVerbs(HttpVerbs.Get)]
        public void CSS(string f, string p)
        {      
            new CssScriptCombiner().Process(f.Split(','),p,
                ControllerContext.RequestContext.HttpContext);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public void JS(string f, string p)
        {
            new JSScriptCombiner().Process(f.Split(','),p,
                ControllerContext.RequestContext.HttpContext);
        }
    }
}
