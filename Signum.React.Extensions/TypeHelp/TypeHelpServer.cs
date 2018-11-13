using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace Signum.React.TypeHelp
{
    public static class TypeHelpServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
        }
    }
}