using Microsoft.AspNetCore.Builder;
using Signum.API;

namespace Signum.Workflow;

public static class WorkflowServer
{
    public static void Start(IApplicationBuilder app)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
    }
}
