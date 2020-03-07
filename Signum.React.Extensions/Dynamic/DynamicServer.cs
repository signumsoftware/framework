using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.React.Json;
using Signum.React.TypeHelp;
using Signum.Utilities;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Signum.React.Facades;
using Signum.Entities.Dynamic;
using Signum.Entities.Authorization;

namespace Signum.React.Dynamic
{
    public static class DynamicServer
    {
        public static void Start(IApplicationBuilder app)
        {
            TypeHelpServer.Start(app);
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
            ReflectionServer.RegisterLike(typeof(DynamicViewMessage), () => UserEntity.Current != null);

            EntityJsonConverter.AfterDeserilization.Register((PropertyRouteEntity wc) =>
            {
                var route = PropertyRouteLogic.TryGetPropertyRouteEntity(wc.RootType, wc.Path);
                if (route != null)
                {
                    wc.SetId(route.Id);
                    wc.SetIsNew(false);
                    wc.SetCleanModified(false);
                }
            });
        }
    }
}
