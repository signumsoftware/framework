using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Web.Extensions.Properties;

namespace Signum.Web.ViewsChecker
{
    public class ViewError
    {
        public static string ViewNameLbl = Resources.ViewName;
        public static string MessageLbl = Resources.Message;
        public static string SourceLbl = Resources.Source;
        public static string StackTraceLbl = Resources.StackTrace;
        public static string TargetSiteLbl = Resources.TargetSite;

        public string ViewName { get; set; }

        public string Message { get; set; }

        public string Source { get; set; }

        public string StackTrace { get; set; }

        public string TargetSite { get; set; }
    }
}
