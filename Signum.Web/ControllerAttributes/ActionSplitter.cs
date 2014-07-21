using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Signum.Web
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class ActionSplitterAttribute : Attribute
    {
        readonly string requestKey;

        public ActionSplitterAttribute(string requestKey)
        {
            this.requestKey = requestKey;
        }

        public string RequestKey
        {
            get { return requestKey; }
        }
    }
}