using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.React.Facades;
using Signum.Utilities;
using System.Web.Http.Dispatcher;
using System.Web.Http.Controllers;
using System.Net.Http;
using System.Web.Http;
using System.Reflection;
using Signum.React.ApiControllers;

namespace Signum.React
{
    public class SignumControllerFactory : DefaultHttpControllerSelector
    {
        public static HashSet<Type> AllowedTypes { get; private set; } = new HashSet<Type>();
        public static Assembly MainAssembly { get; set; }

        public SignumControllerFactory(HttpConfiguration configuration, Assembly mainAssembly) : base(configuration)
        {
        }

        public SignumControllerFactory IncludeOnly<T>(bool only = false)
            where T : ApiController
        {
            AllowedTypes.Add(typeof(T));
            return this;
        }

        public SignumControllerFactory IncludeLike<T>(bool only = false)
            where T : ApiController
        {
            AllowedTypes.AddRange(typeof(T).Assembly.ExportedTypes
                .Where(c => c.Namespace.StartsWith(typeof(T).Namespace) && typeof(ApiController).IsAssignableFrom(c)));

            return this;
        }

        public override IDictionary<string, HttpControllerDescriptor> GetControllerMapping()
        {
            var dic = base.GetControllerMapping();

            var result = dic.Where(a => a.Value.ControllerType.Assembly == MainAssembly || AllowedTypes.Contains(a.Value.ControllerType));

            return dic;
        }
    }
}
