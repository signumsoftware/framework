using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Web.ViewsChecker
{
    public class ViewError
    {
        public string ViewName { get; set; }

        public string Message { get; set; }

        public string Source { get; set; }

        public string StackTrace { get; set; }

        public string TargetSite { get; set; }
    }
}
