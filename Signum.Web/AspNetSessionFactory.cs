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
                get
                {
                    var session = HttpContext.Current.Session;

                    object result = session[Name];

                    if (result != null)
                        return (T)result;

                    if (session.Keys.Cast<string>().Contains(Name))
                        return (T)result;

                    return GetDefaulValue();
                }
                set { HttpContext.Current.Session[Name] = value; }
            }
        }
    }
}