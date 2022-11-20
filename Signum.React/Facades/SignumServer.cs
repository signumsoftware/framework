using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Signum.Engine.Json;
using Signum.Engine.Maps;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.React.ApiControllers;
using Signum.React.Filters;
using Signum.React.JsonModelValidators;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.React.Facades;

public static class SignumServer
{
    public static WebEntityJsonConverterFactory WebEntityJsonConverterFactory = null!;

    static SignumServer()
    {
        WebEntityJsonConverterFactory = new WebEntityJsonConverterFactory();
        WebEntityJsonConverterFactory.CanWritePropertyRoute += EntityJsonConverter_CanWritePropertyRoute;
    }

    public static JsonSerializerOptions JsonSerializerOptions = null!;

    public static JsonOptions AddSignumJsonConverters(this JsonOptions jsonOptions)
    {
        //Signum converters
        jsonOptions.JsonSerializerOptions.IncludeFields = true;
        jsonOptions.JsonSerializerOptions.Do(s =>
        {
            JsonSerializerOptions = s;
            s.WriteIndented = true;
            s.Converters.Add(WebEntityJsonConverterFactory);
            s.Converters.Add(new LiteJsonConverterFactory());
            s.Converters.Add(new MListJsonConverterFactory(WebEntityJsonConverterFactory.AssertCanWrite));
            s.Converters.Add(new JsonStringEnumConverter());
            s.Converters.Add(new ResultTableConverter());
            s.Converters.Add(new TimeSpanConverter());
            s.Converters.Add(new DateOnlyConverter());
            s.Converters.Add(new TimeOnlyConverter());

        });

        return jsonOptions;
    }

    public static MvcOptions AddSignumGlobalFilters(this MvcOptions options)
    {
        options.Filters.Add(new SignumInitializeFilterAttribute());
        options.Filters.Add(new SignumExceptionFilterAttribute());
        options.Filters.Add(new CleanThreadContextAndAssertFilter());
        options.Filters.Add(new SignumEnableBufferingFilter());
        options.Filters.Add(new SignumCurrentContextFilter());
        options.Filters.Add(new SignumTimesTrackerFilter());
        options.Filters.Add(new SignumHeavyProfilerFilter());
        options.Filters.Add(new SignumHeavyProfilerResultFilter());
        options.Filters.Add(new SignumHeavyProfilerActionFilter());
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
        ReflectionServer.RegisterLike(typeof(SearchMessage), () => UserHolder.Current != null);
        ReflectionServer.RegisterLike(typeof(PaginationMode), () => UserHolder.Current != null);
        ReflectionServer.OverrideIsNamespaceAllowed.Add(typeof(DayOfWeek).Namespace!, () => UserHolder.Current != null);
        ReflectionServer.OverrideIsNamespaceAllowed.Add(typeof(CollectionMessage).Namespace!, () => UserHolder.Current != null);
    }

    private static string? EntityJsonConverter_CanWritePropertyRoute(PropertyRoute arg, ModifiableEntity? mod)
    {
        var val = Entities.Validator.TryGetPropertyValidator(arg);

        if (val == null || mod == null)
            return null;

        if (val.IsPropertyReadonly(mod))
            return $"Property {arg} is readonly";

        return null;
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

public class WebEntityJsonConverterFactory : EntityJsonConverterFactory
{
    public override EntityJsonConverterStrategy Strategy => EntityJsonConverterStrategy.WebAPI;

    protected override PropertyRoute GetCurrentPropertyRouteEmbedded(EmbeddedEntity embedded)
    {
        var filterContext = SignumCurrentContextFilter.CurrentContext!;
        var controller = (ControllerActionDescriptor)filterContext.ActionDescriptor;
        var att =
            controller.MethodInfo.GetCustomAttribute< EmbeddedPropertyRouteAttributeBase>() ??
            controller.MethodInfo.DeclaringType!.GetCustomAttribute<EmbeddedPropertyRouteAttributeBase>() ??
            throw new InvalidOperationException(@$"Impossible to determine PropertyRoute for {embedded.GetType().Name}. 
        Consider adding someting like [EmbeddedPropertyRoute<T>] to your action or controller.
        Current action: {controller.MethodInfo.MethodSignature()}
        Current controller: {controller.MethodInfo.DeclaringType!.FullName}");

        return att.GetResolver().GetPropertyRoute(embedded, filterContext);
    }

    public override Type ResolveType(string typeStr, Type objectType, Func<string, Type>? parseType)
    {
        if (Reflector.CleanTypeName(objectType) == typeStr)
            return objectType;

        if (parseType != null)
            return parseType(typeStr);

        var type = ReflectionServer.TypesByName.Value.GetOrThrow(typeStr);

        if (type.IsEnum)
            type = EnumEntity.Generate(type);

        if (!objectType.IsAssignableFrom(type))
            throw new JsonException($"Type '{type.Name}' is not assignable to '{objectType.TypeName()}'");

        return type;
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

public abstract class EmbeddedPropertyRouteAttributeBase : Attribute
{
    public abstract IEmbeddedPropertyRouteResolver GetResolver();
}

[System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
public class EmbeddedPropertyRouteAttribute<T> : EmbeddedPropertyRouteAttributeBase
    where T : class, IEmbeddedPropertyRouteResolver, new()
{
    T? resolver;
    public override IEmbeddedPropertyRouteResolver GetResolver() => resolver ??= new();

    public EmbeddedPropertyRouteAttribute()
    {
    }
}

public interface IEmbeddedPropertyRouteResolver
{
    PropertyRoute GetPropertyRoute(EmbeddedEntity embedded, FilterContext filterContext);
}
