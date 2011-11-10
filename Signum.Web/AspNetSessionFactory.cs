using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Utilities;

namespace Signum.Web
{
    public class AspNetSessionFactory : ISessionFactory
    {
        public IVariable<T> CreateVariable<T>(string name)
        {
            return new AspNetSessionVariable<T>(name);
        }

        class AspNetSessionVariable<T>: IVariable<T>
        {
            string name;

            public AspNetSessionVariable(string name)
            {
                this.name = name;
            }

            public string Name
            {
                get { return this.name; }
            }

            public T Value
            {
                get { return (T)(HttpContext.Current.Session[name] ?? default(T)); }
                set { HttpContext.Current.Session[name] = value; }
            }

            public object UntypedValue
            {
                get { return Value; }
                set { Value = (T)value; }
            }

            public void Clean()
            {
                HttpContext.Current.Session.Remove(name);
            }
        }
    }
}