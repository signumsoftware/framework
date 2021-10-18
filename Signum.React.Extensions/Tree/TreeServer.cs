using Microsoft.AspNetCore.Builder;
using Signum.Engine.Authorization;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Tree;
using Signum.React.Facades;
using System.Linq;
using System.Reflection;

namespace Signum.React.Tree
{
    public class TreeServer
    {
        public static void Start(IApplicationBuilder app)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
            ReflectionServer.RegisterLike(typeof(TreeEntity), () => Schema.Current.Tables.Keys.Where(p => typeof(TreeEntity).IsAssignableFrom(p)).Any(t => TypeAuthLogic.GetAllowed(t).MaxUI() > TypeAllowedBasic.None));
        }
    }
}
