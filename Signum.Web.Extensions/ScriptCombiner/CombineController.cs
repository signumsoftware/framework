using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Web.PortableAreas;

namespace Signum.Web.ScriptCombiner
{
    //TODO: Convert to sessionless controller
    //http://www.lostechies.com/blogs/dahlbyk/archive/2010/12/06/renderaction-with-asp-net-mvc-3-sessionless-controllers.aspx
    public class CombineController : Controller
    {  
        [AcceptVerbs(HttpVerbs.Get)]
        public ScriptContentResult CSS(string key)
        {
            return Combiner.GetContent(key);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ScriptContentResult JS(string key)
        {
            return Combiner.GetContent(key);
        }
    }
}
