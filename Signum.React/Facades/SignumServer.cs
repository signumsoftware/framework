using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.React.ApiControllers;
using Signum.React.Filters;
using Signum.React.Json;
using Signum.React.JsonModelValidators;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Signum.React.Facades
{
    public static class SignumServer
    {
        public static JsonSerializerSettings JsonSerializerSettings = null!;

        public static MvcNewtonsoftJsonOptions AddSignumJsonConverters(this MvcNewtonsoftJsonOptions jsonOptions)
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

            return jsonOptions;
        }

        public static MvcOptions AddSignumGlobalFilters(this MvcOptions options)
        {
            options.Filters.Add(new SignumExceptionFilterAttribute());
            options.Filters.Add(new CleanThreadContextAndAssertFilter());
            options.Filters.Add(new SignumEnableBufferingFilter());
            options.Filters.Add(new SignumCurrentContextFilter());
            options.Filters.Add(new SignumTimesTrackerFilter());
            options.Filters.Add(new SignumHeavyProfilerFilter());
            options.Filters.Add(new SignumAuthenticationFilter());
            options.Filters.Add(new SignumCultureSelectorFilter());
            options.Filters.Add(new VersionFilterAttribute());

            return options;
        }

        public static void AddSignumValidation(this IServiceCollection services)
        {
            services.AddSingleton<IModelMetadataProvider>(s =>
            {
                var modelMetadataProvider = s.GetRequiredService<ICompositeMetadataDetailsProvider>();
                return new SignumModelMetadataProvider(modelMetadataProvider);
            });
            services.AddSingleton<IObjectModelValidator>(s =>
            {
                var options = s.GetRequiredService<IOptions<MvcOptions>>().Value;
                var modelMetadataProvider = s.GetRequiredService<IModelMetadataProvider>();
                return new SignumObjectModelValidator(modelMetadataProvider, options.ModelValidatorProviders);
            });
        }

        public static void Start(IApplicationBuilder app, IWebHostEnvironment hostingEnvironment, Assembly mainAsembly)
        {
            Schema.Current.ApplicationName = hostingEnvironment.ContentRootPath;

            SignumControllerFactory.RegisterArea(typeof(EntitiesController));
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod()!);

            ReflectionServer.Start();
            ReflectionServer.OverrideIsNamespaceAllowed.Add(typeof(DayOfWeek).Namespace!, () => UserHolder.Current != null);
            ReflectionServer.OverrideIsNamespaceAllowed.Add(typeof(CollectionMessage).Namespace!, () => UserHolder.Current != null);
        }

        public static EntityPackTS GetEntityPack(Entity entity)
        {
            var canExecutes = OperationLogic.ServiceCanExecute(entity);

            var result = new EntityPackTS(entity,
                canExecutes.ToDictionary(a => a.Key.Key, a => a.Value)
            );

            if (EntityPackTS.AddExtension != null)
                foreach (var action in EntityPackTS.AddExtension.GetInvocationListTyped())
                {
                    try
                    {
                        action(result);
                    }
                    catch (Exception) when (StartParameters.IgnoredDatabaseMismatches != null)
                    {

                    }
                }

            return result;
        }
    }

    public class EntityPackTS
    {
        public Entity entity { get; set; }
        public Dictionary<string, string> canExecute { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object?> extension { get; set; } = new Dictionary<string, object?>();

        public static Action<EntityPackTS>? AddExtension;

        public EntityPackTS(Entity entity, Dictionary<string, string> canExecute)
        {
            this.entity = entity;
            this.canExecute = canExecute;
        }
    }
}
