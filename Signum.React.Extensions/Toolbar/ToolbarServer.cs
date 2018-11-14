using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace Signum.React.Toolbar
{
    public static class ToolbarServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
        }
    }
}