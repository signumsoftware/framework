using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace Signum.React.Printing
{
    public static class PrintServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

        }
    }
}