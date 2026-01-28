using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Microsoft.Extensions.Logging;
using Signum.API.Filters;
using Signum.API.Json;
using Signum.Entities;
using Signum.Utilities.Reflection;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json;
using static Signum.API.Controllers.OperationController;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Signum.API.Controllers;

[ValidateModelFilter]
public class OperationController : ControllerBase
{
    [HttpPost("api/operation/construct/{operationKey}"), ValidateModelFilter, ProfilerActionSplitter("operationKey")]
    public EntityPackTS? Construct(string operationKey, [Required, FromBody] ConstructOperationRequest request)
    {
        var entityType = TypeLogic.GetType(request.Type);

        var op = request.GetOperationSymbol(operationKey, entityType);

        var entity = OperationLogic.ServiceConstruct(entityType, op, request.ParseArgs(op));

        return entity == null ? null : SignumServer.GetEntityPack(entity);
    }

    [HttpPost("api/operation/constructFromEntity/{operationKey}"), ProfilerActionSplitter("operationKey")]
    public EntityPackTS? ConstructFromEntity(string operationKey, [Required, FromBody] EntityOperationRequest request)
    {
        var op = request.GetOperationSymbol(operationKey, request.entity);

        var entity = OperationLogic.ServiceConstructFrom(request.entity, op, request.ParseArgs(op));

        return entity == null ? null : SignumServer.GetEntityPack(entity);
    }

    [HttpPost("api/operation/constructFromLite/{operationKey}"), ProfilerActionSplitter("operationKey")]
    public EntityPackTS? ConstructFromLite(string operationKey, [Required, FromBody] LiteOperationRequest request)
    {
        var entity = request.lite.Retrieve();
        var op = request.GetOperationSymbol(operationKey, entity);
        var result = OperationLogic.ServiceConstructFrom(entity, op, request.ParseArgs(op));
        return result == null ? null : SignumServer.GetEntityPack(result);
    }

    [HttpPost("api/operation/executeEntity/{operationKey}"), ProfilerActionSplitter("operationKey")]
    public ActionResult<EntityPackTS> ExecuteEntity(string operationKey, [Required, FromBody] EntityOperationRequest request)
    {
        var op = request.GetOperationSymbol(operationKey, request.entity);
        Entity entity;
        try
        {
            entity = OperationLogic.ServiceExecute(request.entity, op, request.ParseArgs(op));
        }
        catch (IntegrityCheckException ex)
        {
            GraphExplorer.SetValidationErrors(GraphExplorer.FromRootVirtual(request.entity), ex);
            this.TryValidateModel(request, "request");
            if (this.ModelState.IsValid)
                throw;

            return BadRequest(this.ModelState);
        }

        return SignumServer.GetEntityPack(entity);
    }


    [HttpPost("api/operation/executeLite/{operationKey}"), ProfilerActionSplitter("operationKey")]
    public EntityPackTS ExecuteLite(string operationKey, [Required, FromBody] LiteOperationRequest request)
    {
        var entity = request.lite.Retrieve();
        var op = request.GetOperationSymbol(operationKey, entity);
        var result = OperationLogic.ServiceExecute(entity, op, request.ParseArgs(op));

        return SignumServer.GetEntityPack(result);
    }

    [HttpPost("api/operation/executeLiteWithProgress/{operationKey}"), ProfilerActionSplitter("operationKey")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProgressStep<EntityPackTS>))]
    [Produces("application/x-ndjson")]
    public Task ExecuteLiteWithProgress(string operationKey, [Required, FromBody] LiteOperationRequest request, CancellationToken cancellationToken)
    {
        var e = request.lite.Retrieve();
        var op = request.GetOperationSymbol(operationKey, e);

        return this.WithProgressProxy(pp =>
        {
            var entity = OperationLogic.ServiceExecute(e, op, request.ParseArgs(op).EmptyIfNull().And(pp).ToArray());
            return SignumServer.GetEntityPack(entity);
        }, cancellationToken);
    }

    [HttpPost("api/operation/deleteEntity/{operationKey}"), ProfilerActionSplitter("operationKey")]
    public void DeleteEntity(string operationKey, [Required, FromBody] EntityOperationRequest request)
    {
        var op = request.GetOperationSymbol(operationKey, request.entity);
        OperationLogic.ServiceDelete(request.entity, op, request.ParseArgs(op));
    }

    [HttpPost("api/operation/deleteLite/{operationKey}"), ProfilerActionSplitter("operationKey")]
    public void DeleteLite(string operationKey, [Required, FromBody] LiteOperationRequest request)
    {
        var e = request.lite.Retrieve();
        var op = request.GetOperationSymbol(operationKey, request.lite.EntityType);
        OperationLogic.ServiceDelete(e, op, request.ParseArgs(op));
    }


    public class ConstructOperationRequest : BaseOperationRequest
    {
        public required string Type { get; set; }
    }

    public class EntityOperationRequest : BaseOperationRequest
    {
        public required Entity entity { get; set; }
    }

    public class LiteOperationRequest : BaseOperationRequest
    {
        public required Lite<Entity> lite { get; set; }
    }

    public class BaseOperationRequest
    {
        public OperationSymbol GetOperationSymbol(string operationKey, Entity entity) => ParseOperationAssert(operationKey, entity.GetType(), entity);
        public OperationSymbol GetOperationSymbol(string operationKey, Type entityType) => ParseOperationAssert(operationKey, entityType, null);

        public static OperationSymbol ParseOperationAssert(string operationKey, Type entityType, Entity? entity)
        {
            var symbol = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

            OperationLogic.AssertOperationAllowed(symbol, entityType, inUserInterface: true, entity: entity);

            return symbol;
        }

        public List<JsonElement>? Args { get; set; }

        public object?[]? ParseArgs(OperationSymbol op)
        {
            return Args == null ? null : Args.Select(a => ConvertObject(a, SignumServer.JsonSerializerOptions, op)).ToArray();
        }


        public static Dictionary<OperationSymbol, Func<JsonElement, object?>> CustomOperationArgsConverters = new Dictionary<OperationSymbol, Func<JsonElement, object?>>();

        public static void RegisterCustomOperationArgsConverter(OperationSymbol operationSymbol, Func<JsonElement, object?> converter)
        {
            Func<JsonElement, object?>? a = CustomOperationArgsConverters.TryGetC(operationSymbol);

            CustomOperationArgsConverters[operationSymbol] = a + converter;
        }

        public static object? ConvertObject(JsonElement token, JsonSerializerOptions? jsonOptions, OperationSymbol? operationSymbol)
        {
            switch (token.ValueKind)
            {
                case JsonValueKind.Undefined: return null;
                case JsonValueKind.String:
                    if (token.TryGetDateTime(out var dt))
                        return dt;

                    if (token.TryGetDateTimeOffset(out var dto))
                        return dto;

                    return token.GetString();
                case JsonValueKind.Number: return token.GetDecimal();
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Null: return null;
                case JsonValueKind.Object:
                    {
                        if (token.TryGetProperty("EntityType", out var entityType))
                            return token.ToObject<Lite<Entity>>(jsonOptions);

                        if (token.TryGetProperty("Type", out var type))
                            return token.ToObject<ModifiableEntity>(jsonOptions);

                        var conv = operationSymbol == null ? null : CustomOperationArgsConverters.TryGetC(operationSymbol);

                        return conv.GetInvocationListTyped().Select(f => f(token)).NotNull().FirstOrDefault();
                    }
                case JsonValueKind.Array:
                    var result = token.EnumerateArray().Select(t => ConvertObject(t, jsonOptions, operationSymbol)).ToList();
                    return result;
                default: 
                    throw new UnexpectedValueException(token.ValueKind);
            }

        }
    }

    [HttpPost("api/operation/constructFromMany/{operationKey}"), ProfilerActionSplitter("operationKey")]
    public EntityPackTS? ConstructFromMany(string operationKey, [Required, FromBody]MultiOperationRequest request)
    {
        var type = request.Lites.Select(l => l.EntityType).Distinct().Only() ?? TypeLogic.GetType(request.Type!);

        var op = request.GetOperationSymbol(operationKey, type);
        var entity = OperationLogic.ServiceConstructFromMany(request.Lites, type, op, request.ParseArgs(op));

        return entity == null ? null : SignumServer.GetEntityPack(entity);
    }

    [HttpPost("api/operation/constructFromMultiple/{operationKey}"), ProfilerActionSplitter("operationKey")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OperationResult))]
    [Produces("application/x-ndjson")]
    public Task ConstructFromMultiple(string operationKey, [Required, FromBody] MultiOperationRequest request, CancellationToken cancellationToken)
    {
        return ForeachNDJson(request.Lites, cancellationToken, async lite =>
        {
            var entity = await lite.RetrieveAsync(cancellationToken);
            if (request.Setters.HasItems())
                MultiSetter.SetSetters(entity, request.Setters, PropertyRoute.Root(entity.GetType()), null);
            var op = request.GetOperationSymbol(operationKey, entity.GetType());
            OperationLogic.ServiceConstructFrom(entity, op, request.ParseArgs(op));
        });
    }


    [HttpPost("api/operation/executeMultiple/{operationKey}"), ProfilerActionSplitter("operationKey")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OperationResult))]
    [Produces("application/x-ndjson")]
    public Task ExecuteMultiple(string operationKey, [Required, FromBody] MultiOperationRequest request, CancellationToken cancellationToken)
    {
        return ForeachNDJson(request.Lites, cancellationToken, async lite =>
        {
            var entity = await lite.RetrieveAsync(cancellationToken);
            if (request.Setters.HasItems())
                MultiSetter.SetSetters(entity, request.Setters, PropertyRoute.Root(entity.GetType()), null);
            var op = request.GetOperationSymbol(operationKey, entity.GetType());
            OperationLogic.ServiceExecute(entity, op, request.ParseArgs(op));
        });
    }


    [HttpPost("api/operation/deleteMultiple/{operationKey}"), ProfilerActionSplitter("operationKey")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OperationResult))]
    [Produces("application/x-ndjson")]
    public Task DeleteMultiple(string operationKey, [Required, FromBody] MultiOperationRequest request, CancellationToken cancellationToken)
    {
        return ForeachNDJson(request.Lites, cancellationToken, async lite =>
        {
            var entity = await lite.RetrieveAsync(cancellationToken);
            if (request.Setters.HasItems())
                MultiSetter.SetSetters(entity, request.Setters, PropertyRoute.Root(entity.GetType()), null);

            var op = request.GetOperationSymbol(operationKey, entity.GetType());
            OperationLogic.ServiceDelete(entity, op, request.ParseArgs(op));
        });
    }

    public class OperationResult
    {
        public Lite<Entity> Entity;
        public string? Error;

        public OperationResult(Lite<Entity> entity)
        {
            Entity = entity;
        }

    }

    public Task ForeachNDJson(IEnumerable<Lite<Entity>> lites, CancellationToken cancellationToken, Func<Lite<Entity>, Task> action) =>
        this.ForeachNDJson(lites, cancellationToken, async lite =>
        {
            try
            {
                await action(lite);
                return new OperationResult(lite);
            }
            catch (Exception e)
            {
                e.Data["lite"] = lite;
                e.LogException();
                return new OperationResult(lite) { Error = e.Message };
            }
        });


    public class ProgressStep<T>
    {
        public string? CurrentTask;
        public int? Min;
        public int? Max;
        public int? Position;


        public bool IsFinished;
        public T? Result;
        public HttpError? Error;
    }

    public class MultiOperationRequest : BaseOperationRequest
    {
        public string? Type { get; set; }
        public required Lite<Entity>[] Lites { get; set; }

        public List<PropertySetter>? Setters { get; set; }
    }

    public class PropertySetter
    {
        public required string Property;
        public PropertyOperation? Operation;
        public FilterOperation? FilterOperation;
        public object? Value;
        public string? EntityType;
        public List<PropertySetter>? Predicate;
        public List<PropertySetter>? Setters;
    }


    public class MultiOperationResponse
    {
        public MultiOperationResponse(Dictionary<string, string> errors)
        {
            this.Errors = errors;
        }

        public Dictionary<string, string> Errors { get; set; }
    }

    [HttpPost("api/operation/stateCanExecutes"), ValidateModelFilter]
    public StateCanExecuteResponse StateCanExecutes([Required, FromBody]StateCanExecuteRequest request)
    {
        var types = request.Lites.Select(a => a.EntityType).ToHashSet();

        var operationSymbols = request.OperationKeys
            .Select(operationKey => types.Select(t => BaseOperationRequest.ParseOperationAssert(operationKey, t, null)).Distinct().SingleEx())
            .ToList();

        var result = OperationLogic.GetContextualCanExecute(request.Lites, operationSymbols);
        var anyReadonly = AnyReadonly.GetInvocationListTyped().Any(f => f(request.Lites));

        return new StateCanExecuteResponse(result.SelectDictionary(a => a.Key, v => v))
        {
            AnyReadonly = anyReadonly
        };
    }


    public static Func<Lite<Entity>[], bool>? AnyReadonly; 

    public class StateCanExecuteRequest
    {
        public required string[] OperationKeys { get; set; }
        public required Lite<Entity>[] Lites { get; set; }
    }

    public class StateCanExecuteResponse
    {
        public StateCanExecuteResponse(Dictionary<string, string> canExecutes)
        {
            this.CanExecutes = canExecutes;
        }

        public bool AnyReadonly;
        public Dictionary<string, string> CanExecutes { get; set; }
    }
}

internal static class MultiSetter
{
    public static void SetSetters(ModifiableEntity entity, List<PropertySetter> setters, PropertyRoute route, SerializationMetadata? metadata)
    {
        if (entity is IRootEntity root)
            metadata = SignumServer.WebEntityJsonConverterFactory.GetSerializationMetadata?.Invoke(root);

        var options = SignumServer.JsonSerializerOptions;

        foreach (var setter in setters)
        {
            var pr = route.AddMany(setter.Property);

            if (pr.Parent!.Type.IsMixinEntity())
                SignumServer.WebEntityJsonConverterFactory.AssertCanWrite(pr, pr.Parent.GetLambdaExpression<ModifiableEntity, MixinEntity>(false).Compile()(entity), metadata);
            else
                SignumServer.WebEntityJsonConverterFactory.AssertCanWrite(pr, entity, metadata);

            if (pr.Type.IsMList())
            {
                var elementPr = pr.Add("Item");

                var mlist = pr.GetLambdaExpression<ModifiableEntity, IMListPrivate>(false).Compile()(entity);
                switch (setter.Operation)
                {
                    case PropertyOperation.AddElement:
                        {
                            var value = ConvertObject(setter.Value, elementPr, options);
                            ((IList)mlist).Add(value);
                        }
                        break;
                    case PropertyOperation.AddNewElement:
                        {
                            var item = (ModifiableEntity)Activator.CreateInstance(elementPr.Type)!;
                            var normalizedPr = elementPr.Type.IsEntity() ? PropertyRoute.Root(elementPr.Type) : elementPr;
                                
                            SetSetters(item, setter.Setters!, normalizedPr, metadata);
                            ((IList)mlist).Add(item);
                        }
                        break;
                    case PropertyOperation.ChangeElements:
                        {
                            var predicate = GetPredicate(setter.Predicate!, elementPr, options);
                            var toChange = ((IEnumerable<object>)mlist).Where(predicate.Compile()).ToList();
                            var normalizedPr = elementPr.Type.IsEntity() ? PropertyRoute.Root(elementPr.Type) : elementPr;
                            foreach (var item in toChange)
                            {
                                SetSetters((ModifiableEntity)item, setter.Setters!, normalizedPr, metadata);
                            }
                        }
                        break;
                    case PropertyOperation.RemoveElementsWhere:
                        {
                            var predicate = GetPredicate(setter.Predicate!, elementPr, options);
                            var toRemove = ((IEnumerable<object>)mlist).Where(predicate.Compile()).ToList();
                            foreach (var item in toRemove)
                            {
                                ((IList)mlist).Remove(item);
                            }
                        }
                        break;
                    case PropertyOperation.RemoveElement:
                        {
                            var value = ConvertObject(setter.Value, elementPr, options);
                            ((IList)mlist).Remove(value);
                        }
                        break;
                    default:
                        break;
                }
            }
            else if (setter.Operation == PropertyOperation.CreateNewEntity)
            {
                var subPr = pr.Type.IsEmbeddedEntity() ? pr : PropertyRoute.Root(TypeLogic.GetType(setter.EntityType!));
                var item = (ModifiableEntity)Activator.CreateInstance(subPr.Type)!;
                SetSetters(item, setter.Setters!, subPr, metadata);
                SetProperty(entity, pr, route, item);
            }
            else if (setter.Operation == PropertyOperation.ModifyEntity)
            {
                var item = GetProperty(entity, pr, route);
                if (!(item is ModifiableEntity mod))
                    throw new InvalidOperationException($"Unable to change entity in {pr}: {item}");

                SetSetters(mod, setter.Setters!, pr, metadata);
                SetProperty(entity, pr, route, mod);
            }
            else if (setter.Operation == PropertyOperation.Set)
            {
                var value = ConvertObject(setter.Value, pr, options);
                SetProperty(entity, pr, route, value);
            }
            else
            {
                throw new UnexpectedValueException(setter.Operation);
            }
        }
    }

    private static void SetProperty(ModifiableEntity entity, PropertyRoute pr, PropertyRoute parentRoute, object? value)
    {
        var subEntity = pr.Parent == parentRoute ? entity :
                    (ModifiableEntity)pr.Parent!.GetLambdaExpression<object, object>(true, parentRoute).Compile()(entity);

        pr.PropertyInfo!.SetValue(subEntity, value);
    }

    private static object? GetProperty(ModifiableEntity entity, PropertyRoute pr, PropertyRoute parentRoute)
    {
        var subEntity = pr.Parent == parentRoute ? entity :
                    (ModifiableEntity)pr.Parent!.GetLambdaExpression<object, object>(true, parentRoute).Compile()(entity);

        return pr.PropertyInfo!.GetValue(subEntity);
    }


    static Expression<Func<object, bool>> GetPredicate(List<PropertySetter> predicate, PropertyRoute mainRoute, JsonSerializerOptions options)
    {
        var param = Expression.Parameter(typeof(object), "p");


        var body = predicate.IsEmpty() ? Expression.Constant(true) : predicate.Select(p =>
        {
            var pr = mainRoute.AddMany(p.Property);

            var lambda = pr.GetLambdaExpression<object, object>(true, mainRoute.GetMListItemsRoute());

            var left = Expression.Invoke(lambda, param);
            object? objClean = ConvertObject(p.Value, pr, options);

            return (Expression)QueryUtils.GetCompareExpression(p.FilterOperation!.Value, left, Expression.Constant(objClean), inMemory: true);

        }).Aggregate((a, b) => Expression.AndAlso(a, b));

        return Expression.Lambda<Func<object, bool>>(body, param);
    }

    private static object? ConvertObject(object? value, PropertyRoute pr, JsonSerializerOptions options)
    {
        var objRaw = value == null ? null :
                        value is JsonElement elem ? elem.ToObject(pr.Type, options) :
                        value;

        var objClean = ReflectionTools.ChangeType(objRaw, pr.Type);
        return objClean;
    }
}

public static class ControllerProgressExtension
{
    public static async Task WithProgressProxy<T>(this ControllerBase controller,   Func<ProgressProxy, T> action, CancellationToken cancellationToken)
    {
        EventWaitHandle handle = new EventWaitHandle(false, EventResetMode.AutoReset);

        ProgressStep<T>? lastProgress = null;

        var context = controller.ControllerContext;
        var httpContext = controller.HttpContext;

        var task = Task.Run(() =>
        {
            ProgressProxy pp = new ProgressProxy(cancellationToken);

            pp.Changed += (sender, p) =>
            {
                lastProgress = new ProgressStep<T>
                {
                    CurrentTask = pp.CurrentTask,
                    Max = pp.Max,
                    Min = pp.Min,
                    Position = pp.Position,
                };
                handle.Set();
            };

            try
            {
                var result = action(pp);
                lastProgress = new ProgressStep<T>
                {
                    IsFinished = true,
                    Result = result,
                };
                handle.Set();
            }
            catch (Exception ex)
            {
                SignumExceptionFilterAttribute.LogException(ex, context).Wait();
                var error = SignumExceptionFilterAttribute.CustomHttpErrorFactory(new ResourceExecutedContext(context, []) { Exception = ex });
                lastProgress = new ProgressStep<T>
                {
                    IsFinished = true,
                    Error = error
                };
                handle.Set();
            }
        });

        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            IncludeFields = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };


        options.Converters.AddRange(SignumServer.JsonSerializerOptions.Converters);

        while (await handle.WaitOneAsync(CancellationToken.None))
        {
            var lp = lastProgress;
            if (lp == null) //Exception
                break;
            else
            {
                var json = JsonSerializer.Serialize(lp, options);
                if (json.Contains("\n"))
                    throw new InvalidOperationException("\n in Json object found!");

                await httpContext.Response.WriteAsync(json + "\n");
                await httpContext.Response.Body.FlushAsync();

                if (lp.IsFinished)
                    break;
            }
        }

        //await task; //avoid throwing the exception
    }

    public static async Task ForeachNDJson<T, TResult>(this ControllerBase controller, IEnumerable<T> lites, CancellationToken cancellationToken, Func<T, Task<TResult>> action)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            IncludeFields = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        options.Converters.AddRange(SignumServer.JsonSerializerOptions.Converters);

        var context = controller.HttpContext;
        foreach (var lite in lites.Distinct())
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var s = await action(lite);

            var json = JsonSerializer.Serialize(s, options);
            if (json.Contains("\n"))
                throw new InvalidOperationException("\n in Json object found!");

            await context.Response.WriteAsync(json + "\n");
            await context.Response.Body.FlushAsync();
        }
    }
}
