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
        public Variable<T> CreateVariable<T>(string name)
        {
            return new AspNetSessionVariable<T>(name);
        }

        class AspNetSessionVariable<T>: Variable<T>
        {
            public AspNetSessionVariable(string name): base(name)
            {
            }

            public override T Value
            {
                get { return (T)(HttpContext.Current.Session[Name] ?? default(T)); }
                set { HttpContext.Current.Session[Name] = value; }
            }
        }
    }
}