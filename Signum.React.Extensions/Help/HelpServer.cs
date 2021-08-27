using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace Signum.React.Help
{
    public static class HelpServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
        }
    }
}
