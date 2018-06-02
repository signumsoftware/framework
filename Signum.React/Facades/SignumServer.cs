using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
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
namespace Signum.React.Facades
{
    public static class SignumServer
    {
        public static JsonSerializerSettings JsonSerializerSettings;
        
        public static void AddSignumJsonConverters(this MvcJsonOptions jsonOptions)
        {
            //Signum converters
            jsonOptions.SerializerSettings.Do(s =>
            {
                JsonSerializerSettings = s;

                s.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                s.Formatting = Newtonsoft.Json.Formatting.Indented;
                s.Converters.Add(new LiteJsonConverter());
                s.Converters.Add(new EntityJsonConverter());
                s.Converters.Add(new MListJsonConverter());
                s.Converters.Add(new StringEnumConverter());
                s.Converters.Add(new ResultTableConverter());
                s.Converters.Add(new TimeSpanConverter());
            });
        }

        public static void AddSignumGlobalFilters(this MvcOptions options)
        {
            options.Filters.Add(new SignumEnableBufferingFilter());
            options.Filters.Add(new SignumTimesTrackerFilter());
            options.Filters.Add(new SignumHeavyProfilerFilter());
            options.Filters.Add(new SignumAuthenticationFilter());
            options.Filters.Add(new SignumCultureSelectorFilter());
            options.Filters.Add(new SignumExceptionFilterAttribute());
            options.Filters.Add(new VersionFilterAttribute());
        }
        
        public static void Start(IApplicationBuilder app, IHostingEnvironment hostingEnvironment, Assembly mainAsembly)
        {
            Schema.Current.ApplicationName = hostingEnvironment.ContentRootPath;

            //app.Services.Replace(typeof(IHttpControllerSelector), new SignumControllerFactory(config, mainAsembly));

            SignumControllerFactory.RegisterArea(typeof(EntitiesController));
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());


            //// Web API configuration and services
            //var appXmlType = app.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault(t => t.MediaType == "application/xml");
            //app.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);


            //// Web API routes
            //app.MapHttpAttributeRoutes();

            //app.Services.Replace(typeof(IBodyModelValidator), new SignumBodyModelValidator());

            
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