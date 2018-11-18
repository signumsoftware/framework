using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace Signum.React.RestLog
{
    public static class RestLogServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());


        }
    }
}
