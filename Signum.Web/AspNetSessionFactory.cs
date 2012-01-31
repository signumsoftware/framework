using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Utilities;
using System.Security.Principal;
using System.Threading;

namespace Signum.Web
{
    public class AspNetSessionFactory : ISessionFactory
    {
        public SessionVariable<T> CreateVariable<T>(string name)
        {
            return new AspNetSessionVariable<T>(name);
        }

        class AspNetSessionVariable<T>: SessionVariable<T>
        {
            public AspNetSessionVariable(string name): base(name)
            {
            }

            public override Func<T> ValueFactory { get; set; }

            public override T Value
            {
                get { return (T)(HttpContext.Current.Session[Name] ?? GetDefaulValue()); }
                set { HttpContext.Current.Session[Name] = value; }
            }
        }
    }
}