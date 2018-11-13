using System.Reflection;
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