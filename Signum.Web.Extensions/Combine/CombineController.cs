using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Web.PortableAreas;
using System.Web.SessionState;

namespace Signum.Web.Combine
{
    [SessionState(SessionStateBehavior.Disabled), AuthenticationRequired(false)]
    public class CombineController : Controller
    {  
        [AcceptVerbs(HttpVerbs.Get)]
        public StaticContentResult CSS(string key)
        {
            return CombineClient.GetContent(key);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public StaticContentResult JS(string key)
        {
            return CombineClient.GetContent(key);
        }
    }
}
