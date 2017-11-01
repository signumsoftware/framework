using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.React;
using Signum.React.Json;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;

namespace Signum.React.TypeHelp
{
    public static class TypeHelpServer
    {
        public static void Start(HttpConfiguration config)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
        }
    }
}