using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Utilities;
using System.Security.Principal;
using System.Threading;
using System.Web.SessionState;

namespace Signum.Web
{
    public class AspNetSessionFactory : ISessionFactory
    {
        static readonly ThreadVariable<HttpSessionState> overrideAspNetSessionVariable = Statics.ThreadVariable<HttpSessionState>("overrideASPNetSession");
        //Usefull for Session_End  http://stackoverflow.com/questions/464456/httpcontext-current-session-vs-global-asax-this-session
        public static IDisposable OverrideAspNetSession(HttpSessionState globalAsaxSession)
        {
            if (overrideAspNetSessionVariable.Value != null)
                throw new InvalidOperationException("overrideASPNetSession is already set");

            overrideAspNetSessionVariable.Value = globalAsaxSession;
            return new Disposable(() => overrideAspNetSessionVariable.Value = null);
        }

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
                    var session = overrideAspNetSessionVariable.Value ?? HttpContext.Current?.Session;

                    if (session == null)
                        return default(T);

                    object result = session[Name];

                    if (result != null)
                        return (T)result;

                    if (session.Keys.Cast<string>().Contains(Name))
                        return (T)result;

                    return GetDefaulValue();
                }
                set { (overrideAspNetSessionVariable.Value ?? HttpContext.Current.Session)[Name] = value; }
            }

            public override void Clean()
            {
                var session = overrideAspNetSessionVariable.Value ?? HttpContext.Current?.Session;
                session.Remove(Name);
            }
        }
    }
}