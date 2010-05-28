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
            new JsScriptCombiner().Process(f.Split(','),p,
                ControllerContext.RequestContext.HttpContext);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public void AreaJs(string f)
        {
            new AreaJsScriptCombiner()
                .Process(f.Split(','), null, ControllerContext.RequestContext.HttpContext);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public void AreaCss(string f)
        {
            new AreaCssScriptCombiner()
                .Process(f.Split(','), null, ControllerContext.RequestContext.HttpContext);
        }
    }
}
