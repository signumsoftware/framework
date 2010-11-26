using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;

namespace Signum.Web.ScriptCombiner
{
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
