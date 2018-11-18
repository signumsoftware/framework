using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace Signum.React.Processes
{
    public static class ProcessServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

        }
    }
}