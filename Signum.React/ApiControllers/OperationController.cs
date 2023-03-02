using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.React.Facades;
using Signum.React.Filters;
using Signum.Utilities.Reflection;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Signum.React.ApiControllers.OperationController;

namespace Signum.React.ApiControllers;

[ValidateModelFilter]
public class OperationController : Controller
{
    [HttpPost("api/operation/construct"), ValidateModelFilter, ProfilerActionSplitter]
    public EntityPackTS? Construct([Required, FromBody] ConstructOperationRequest request)
    {
        var entityType = TypeLogic.GetType(request.Type);

        var op = request.GetOperationSymbol(entityType);

        var entity = OperationLogic.ServiceConstruct(entityType, op, request.ParseArgs(op));

        return entity == null ? null : SignumServer.GetEntityPack(entity);
    }

    [HttpPost("api/operation/constructFromEntity"), ProfilerActionSplitter]
    public EntityPackTS? ConstructFromEntity([Required, FromBody] EntityOperationRequest request)
    {
        var op = request.GetOperationSymbol(request.entity.GetType());

        var entity = OperationLogic.ServiceConstructFrom(request.entity, op, request.ParseArgs(op));

        return entity == null ? null : SignumServer.GetEntityPack(entity);
    }

    [HttpPost("api/operation/constructFromLite"), ProfilerActionSplitter]
    public EntityPackTS? ConstructFromLite([Required, FromBody] LiteOperationRequest request)
    {
        var op = request.GetOperationSymbol(request.lite.EntityType);
        var entity = OperationLogic.ServiceConstructFromLite(request.lite, op, request.ParseArgs(op));
        return entity == null ? null : SignumServer.GetEntityPack(entity);
    }


    [HttpPost("api/operation/executeEntity"), ProfilerActionSplitter]
    public ActionResult<EntityPackTS> ExecuteEntity([Required, FromBody] EntityOperationRequest request)
    {
        var op = request.GetOperationSymbol(request.entity.GetType());
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


    [HttpPost("api/operation/executeLite"), ProfilerActionSplitter]
    public EntityPackTS ExecuteLite([Required, FromBody] LiteOperationRequest request)
    {
        var op = request.GetOperationSymbol(request.lite.EntityType);
        var entity = OperationLogic.ServiceExecuteLite(request.lite, op, request.ParseArgs(op));

        return SignumServer.GetEntityPack(entity);
    }

    [HttpPost("api/operation/deleteEntity"), ProfilerActionSplitter]
    public void DeleteEntity([Required, FromBody] EntityOperationRequest request)
    {
        var op = request.GetOperationSymbol(request.entity.GetType());
        OperationLogic.ServiceDelete(request.entity, op, request.ParseArgs(op));
    }

    [HttpPost("api/operation/deleteLite"), ProfilerActionSplitter]
    public void DeleteLite([Required, FromBody] LiteOperationRequest request)
    {
        var op = request.GetOperationSymbol(request.lite.EntityType);
        OperationLogic.ServiceDelete(request.lite, op, request.ParseArgs(op));
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
        public required string OperationKey { get; set; }

        public OperationSymbol GetOperationSymbol(Type entityType) => ParseOperationAssert(this.OperationKey, entityType);

        public static OperationSymbol ParseOperationAssert(string operationKey, Type entityType)
        {
            var symbol = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

            OperationLogic.AssertOperationAllowed(symbol, entityType, inUserInterface: true);

            return symbol;
        }

        public List<JsonElement>? Args { get; set; }

        public object?[]? ParseArgs(OperationSymbol op)
        {
            return Args == null ? null : Args.Select(a => ConvertObject(a, op)).ToArray();
        }


        public static Dictionary<OperationSymbol, Func<JsonElement, object?>> CustomOperationArgsConverters = new Dictionary<OperationSymbol, Func<JsonElement, object?>>();

        public static void RegisterCustomOperationArgsConverter(OperationSymbol operationSymbol, Func<JsonElement, object?> converter)
        {
            Func<JsonElement, object?>? a = CustomOperationArgsConverters.TryGetC(operationSymbol); /*CSBUG*/

            CustomOperationArgsConverters[operationSymbol] = a + converter;
        }

        public static object? ConvertObject(JsonElement token, OperationSymbol? operationSymbol)
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
                            return token.ToObject<Lite<Entity>>(SignumServer.JsonSerializerOptions);

                        if (token.TryGetProperty("Type", out var type))
                            return token.ToObject<ModifiableEntity>(SignumServer.JsonSerializerOptions);

                        var conv = operationSymbol == null ? null : CustomOperationArgsConverters.TryGetC(operationSymbol);

                        return conv.GetInvocationListTyped().Select(f => f(token)).NotNull().FirstOrDefault();
                    }
                case JsonValueKind.Array:
                    var result = token.EnumerateArray().Select(t => ConvertObject(t, operationSymbol)).ToList();
                    return result;
                default: 
                    throw new UnexpectedValueException(token.ValueKind);
            }

        }

        public override string ToString() => OperationKey;
    }

    [HttpPost("api/operation/constructFromMany"), ProfilerActionSplitter]
    public EntityPackTS? ConstructFromMany([Required, FromBody]MultiOperationRequest request)
    {
        var type = request.Lites.Select(l => l.EntityType).Distinct().Only() ?? TypeLogic.GetType(request.Type!);

        var op = request.GetOperationSymbol(type);
        var entity = OperationLogic.ServiceConstructFromMany(request.Lites, type, op, request.ParseArgs(op));

        return entity == null ? null : SignumServer.GetEntityPack(entity);
    }

    [HttpPost("api/operation/constructFromMultiple"), ProfilerActionSplitter]
    public IAsyncEnumerable<OperationResult> ConstructFromMultiple([Required, FromBody] MultiOperationRequest request, CancellationToken cancellationToken)
    {
        return ForeachMultiple(request.Lites, async lite =>
        {
            var entity = await lite.RetrieveAsync(cancellationToken);
            if (request.Setters.HasItems())
                MultiSetter.SetSetters(entity, request.Setters, PropertyRoute.Root(entity.GetType()));
            var op = request.GetOperationSymbol(entity.GetType());
            OperationLogic.ServiceConstructFrom(entity, op, request.ParseArgs(op));
        }, cancellationToken);
    }


    [HttpPost("api/operation/executeMultiple"), ProfilerActionSplitter]
    public IAsyncEnumerable<OperationResult> ExecuteMultiple([Required, FromBody] MultiOperationRequest request, CancellationToken cancellationToken)
    {
        return ForeachMultiple(request.Lites, async lite =>
        {
            var entity = await lite.RetrieveAsync(cancellationToken);
            if (request.Setters.HasItems())
                MultiSetter.SetSetters(entity, request.Setters, PropertyRoute.Root(entity.GetType()));
            var op = request.GetOperationSymbol(entity.GetType());
            OperationLogic.ServiceExecute(entity, op, request.ParseArgs(op));
        }, cancellationToken);
    }


    [HttpPost("api/operation/deleteMultiple"), ProfilerActionSplitter]
    public IAsyncEnumerable<OperationResult> DeleteMultiple([Required, FromBody] MultiOperationRequest request, CancellationToken cancellationToken)
    {
        return ForeachMultiple(request.Lites, async lite =>
        {
            var entity = await lite.RetrieveAsync(cancellationToken);
            if (request.Setters.HasItems())
                MultiSetter.SetSetters(entity, request.Setters, PropertyRoute.Root(entity.GetType()));

            var op = request.GetOperationSymbol(entity.GetType());
            OperationLogic.ServiceDelete(entity, op, request.ParseArgs(op));
        }, cancellationToken);
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

    async static IAsyncEnumerable<OperationResult> ForeachMultiple(IEnumerable<Lite<Entity>> lites, Func<Lite<Entity>, Task> action, [EnumeratorCancellation]CancellationToken cancellationToken)
    {
        foreach (var lite in lites.Distinct())
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            string? error = null;
            try
            {
                await action(lite);
            }
            catch (Exception e)
            {
                e.Data["lite"] = lite;
                e.LogException();
                error = e.Message;
            }
            yield return new OperationResult(lite) { Error = error };
        }
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
            .Select(operationKey => types.Select(t => BaseOperationRequest.ParseOperationAssert(operationKey, t)).Distinct().SingleEx())
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
    public static void SetSetters(ModifiableEntity entity, List<PropertySetter> setters, PropertyRoute route)
    {
        var options = SignumServer.JsonSerializerOptions;

        foreach (var setter in setters)
        {
            var pr = route.AddMany(setter.Property);

            SignumServer.WebEntityJsonConverterFactory.AssertCanWrite(pr, entity);

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
                                
                            SetSetters(item, setter.Setters!, normalizedPr);
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
                                SetSetters((ModifiableEntity)item, setter.Setters!, normalizedPr);
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
                SetSetters(item, setter.Setters!, subPr);
                SetProperty(entity, pr, route, item);
            }
            else if (setter.Operation == PropertyOperation.ModifyEntity)
            {
                var item = GetProperty(entity, pr, route);
                if (!(item is ModifiableEntity mod))
                    throw new InvalidOperationException($"Unable to change entity in {pr}: {item}");

                SetSetters(mod, setter.Setters!, pr);
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

        var body = predicate.Select(p =>
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
