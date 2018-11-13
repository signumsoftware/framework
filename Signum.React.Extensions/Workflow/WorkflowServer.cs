using Signum.React.Json;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Signum.Engine.Basics;
using Signum.React.UserAssets;
using Microsoft.AspNetCore.Builder;

namespace Signum.React.Workflow
{
    public static class WorkflowServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());            
        }
    }
}