using Microsoft.AspNetCore.Builder;
using Signum.Entities.Tree;
using Signum.React.Facades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;

namespace Signum.React.Tree
{
    public class TreeServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
            ReflectionServer.RegisterLike(typeof(TreeEntity));
        }
    }
}