using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.React.ApiControllers;
using Signum.React.Filters;
using Signum.React.Json;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.Validation;
using System.Web.Routing;

namespace Signum.React.Facades
{
    public static class SignumServer
    {
        public static void Start(HttpConfiguration config, Assembly mainAsembly)
        {
            Schema.Current.ApplicationName = System.Web.Hosting.HostingEnvironment.ApplicationHost.GetPhysicalPath();

            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.Services.Replace(typeof(IHttpControllerSelector), new SignumControllerFactory(config, mainAsembly));

            SignumControllerFactory.RegisterArea(typeof(EntitiesController));
            // Web API configuration and services
            var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault(t => t.MediaType == "application/xml");
            config.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);

            //Signum converters
            config.Formatters.JsonFormatter.SerializerSettings.Do(s =>
            {
                s.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                s.Formatting = Newtonsoft.Json.Formatting.Indented;
                s.Converters.Add(new LiteJsonConverter());
                s.Converters.Add(new EntityJsonConverter());
                s.Converters.Add(new MListJsonConverter());
                s.Converters.Add(new StringEnumConverter());
                s.Converters.Add(new ResultTableConverter());
                s.Converters.Add(new TimeSpanConverter());
            });

            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());


            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Services.Replace(typeof(IBodyModelValidator), new SignumBodyModelValidator());

            config.Filters.Add(new SignumAuthenticationFilterAttribute());
            config.Filters.Add(new SignumAuthorizationFilterAttribute());
            config.Filters.Add(new SignumExceptionFilterAttribute());
            config.Filters.Add(new VersionFilterAttribute());
            
            ReflectionServer.Start();
        }

        
        public static EntityPackTS GetEntityPack(Entity entity)
        {
            var canExecutes = OperationLogic.ServiceCanExecute(entity);

            var result = new EntityPackTS
            {
                entity = entity,
                canExecute = canExecutes.ToDictionary(a => a.Key.Key, a => a.Value)
            };

            foreach (var action in EntityPackTS.AddExtension.GetInvocationListTyped())
            {
                action(result);
            }

            return result;
        }
    }

    public class EntityPackTS
    {
        public Entity entity { get; set; }
        public Dictionary<string, string> canExecute { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Extension { get; set; } = new Dictionary<string, object>();

        public static Action<EntityPackTS> AddExtension;
    }
}