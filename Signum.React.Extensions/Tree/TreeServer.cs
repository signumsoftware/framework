using Microsoft.AspNetCore.Builder;
using Signum.Entities.Tree;
using Signum.React.Facades;
using System.Reflection;

namespace Signum.React.Tree
{
    public class TreeServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
            ReflectionServer.RegisterLike(typeof(TreeEntity));
        }
    }
}