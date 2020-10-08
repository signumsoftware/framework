using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.React.Facades;
using Signum.React.Filters;
using Signum.React.Json;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static Signum.React.ApiControllers.OperationController;

namespace Signum.React.ApiControllers
{
    [ValidateModelFilter]
    public class OperationController : Controller
    {
        [HttpPost("api/operation/construct"), ValidateModelFilter, ProfilerActionSplitter]
        public EntityPackTS? Construct([Required, FromBody] ConstructOperationRequest request)
        {
            var entityType = TypeLogic.GetType(request.Type);

            var entity = OperationLogic.ServiceConstruct(entityType, request.GetOperationSymbol(entityType), request.Args);

            return entity == null ? null : SignumServer.GetEntityPack(entity);
        }

        [HttpPost("api/operation/constructFromEntity"), ProfilerActionSplitter]
        public EntityPackTS? ConstructFromEntity([Required, FromBody] EntityOperationRequest request)
        {
            var entity = OperationLogic.ServiceConstructFrom(request.entity, request.GetOperationSymbol(request.entity.GetType()), request.Args);

            return entity == null ? null : SignumServer.GetEntityPack(entity);
        }

        [HttpPost("api/operation/constructFromLite"), ProfilerActionSplitter]
        public EntityPackTS? ConstructFromLite([Required, FromBody] LiteOperationRequest request)
        {
            var entity = OperationLogic.ServiceConstructFromLite(request.lite, request.GetOperationSymbol(request.lite.EntityType), request.Args);
            return entity == null ? null : SignumServer.GetEntityPack(entity);
        }


        [HttpPost("api/operation/executeEntity"), ProfilerActionSplitter]
        public ActionResult<EntityPackTS> ExecuteEntity([Required, FromBody] EntityOperationRequest request)
        {
            Entity entity;
            try
            {
                entity = OperationLogic.ServiceExecute(request.entity, request.GetOperationSymbol(request.entity.GetType()), request.Args);
            }
            catch (IntegrityCheckException ex)
            {
                GraphExplorer.SetValidationErrors(GraphExplorer.FromRootVirtual(request.entity), ex);
                this.TryValidateModel(request, "request");
                if (this.ModelState.IsValid)
                    throw ex;
                return BadRequest(this.ModelState);
            }

            return SignumServer.GetEntityPack(entity);
        }


        [HttpPost("api/operation/executeLite"), ProfilerActionSplitter]
        public EntityPackTS ExecuteLite([Required, FromBody] LiteOperationRequest request)
        {
            var entity = OperationLogic.ServiceExecuteLite(request.lite, request.GetOperationSymbol(request.lite.EntityType), request.Args);

            return SignumServer.GetEntityPack(entity);
        }

        [HttpPost("api/operation/deleteEntity"), ProfilerActionSplitter]
        public void DeleteEntity([Required, FromBody] EntityOperationRequest request)
        {
            OperationLogic.ServiceDelete(request.entity, request.GetOperationSymbol(request.entity.GetType()), request.Args);
        }

        [HttpPost("api/operation/deleteLite"), ProfilerActionSplitter]
        public void DeleteLite([Required, FromBody] LiteOperationRequest request)
        {
            OperationLogic.ServiceDelete(request.lite, request.GetOperationSymbol(request.lite.EntityType), request.Args);
        }


#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        [JsonConverter(typeof(ArgsJsonConverter))]
        public class ConstructOperationRequest : BaseOperationRequest
        {
            public string Type { get; set; }
        }


        [JsonConverter(typeof(ArgsJsonConverter))]
        public class EntityOperationRequest : BaseOperationRequest
        {
            public Entity entity { get; set; }
        }


        [JsonConverter(typeof(ArgsJsonConverter))]
        public class LiteOperationRequest : BaseOperationRequest
        {
            public Lite<Entity> lite { get; set; }
        }

        [JsonConverter(typeof(ArgsJsonConverter))]
        public class BaseOperationRequest
        {
            public string OperationKey { get; set; }

            public object?[]? Args { get; set; }

            public OperationSymbol GetOperationSymbol(Type entityType) => ParseOperationAssert(this.OperationKey, entityType, this.Args);

            public static OperationSymbol ParseOperationAssert(string operationKey, Type entityType, object?[]? args = null)
            {
                var symbol = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

                OperationLogic.AssertOperationAllowed(symbol, entityType, inUserInterface: true);

                return symbol;
            }

            public override string ToString() => OperationKey;
        }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

        [HttpPost("api/operation/constructFromMany"), ProfilerActionSplitter]
        public EntityPackTS? ConstructFromMany([Required, FromBody]MultiOperationRequest request)
        {
            var type = request.Lites.Select(l => l.EntityType).Distinct().Only() ?? TypeLogic.GetType(request.Type);

            var entity = OperationLogic.ServiceConstructFromMany(request.Lites, type, request.GetOperationSymbol(type), request.Args);

            return entity == null ? null : SignumServer.GetEntityPack(entity);
        }

        [HttpPost("api/operation/constructFromMultiple"), ProfilerActionSplitter]
        public MultiOperationResponse ConstructFromMultiple([Required, FromBody] MultiOperationRequest request)
        {
            if (request.Setters.HasItems())
            {
                var errors = ForeachMultiple(request.Lites, lite =>
                {
                    var entity = lite.RetrieveAndForget();

                    MultiSetter.SetSetters(entity, request.Setters, PropertyRoute.Root(entity.GetType()));

                    OperationLogic.ServiceConstructFrom(entity, request.GetOperationSymbol(entity.GetType()), request.Args);
                });

                return new MultiOperationResponse(errors);
            }
            else
            {
                var errors = ForeachMultiple(request.Lites, lite =>
                            OperationLogic.ServiceConstructFromLite(lite, request.GetOperationSymbol(lite.EntityType), request.Args));

                return new MultiOperationResponse(errors);
            }
        }


        [HttpPost("api/operation/executeMultiple"), ProfilerActionSplitter]
        public MultiOperationResponse ExecuteMultiple([Required, FromBody] MultiOperationRequest request)
        {
            if (request.Setters.HasItems())
            {
                var errors = ForeachMultiple(request.Lites, lite =>
                {
                    var entity = lite.RetrieveAndForget();

                    MultiSetter.SetSetters(entity, request.Setters, PropertyRoute.Root(entity.GetType()));

                    OperationLogic.ServiceExecute(entity, request.GetOperationSymbol(entity.GetType()), request.Args);
                });

                return new MultiOperationResponse(errors);
            }
            else
            {
                var errors = ForeachMultiple(request.Lites, lite =>
                            OperationLogic.ServiceExecuteLite(lite, request.GetOperationSymbol(lite.EntityType), request.Args));

                return new MultiOperationResponse(errors);
            }
        }


        [HttpPost("api/operation/deleteMultiple"), ProfilerActionSplitter]
        public MultiOperationResponse DeleteMultiple([Required, FromBody] MultiOperationRequest request)
        {
            if (request.Setters.HasItems())
            {
                var errors = ForeachMultiple(request.Lites, lite =>
                {
                    var entity = lite.RetrieveAndForget();

                    MultiSetter.SetSetters(entity, request.Setters, PropertyRoute.Root(entity.GetType()));

                    OperationLogic.ServiceDelete(entity, request.GetOperationSymbol(entity.GetType()), request.Args);
                });

                return new MultiOperationResponse(errors);
            }
            else
            {
                var errors = ForeachMultiple(request.Lites, lite =>
                            OperationLogic.ServiceDelete(lite, request.GetOperationSymbol(lite.EntityType), request.Args));

                return new MultiOperationResponse(errors);
            }
        }

        static Dictionary<string, string> ForeachMultiple(IEnumerable<Lite<Entity>> lites, Action<Lite<Entity>> action)
        {
            Dictionary<string, string> errors = new Dictionary<string, string>();
            foreach (var lite in lites.Distinct())
            {
                try
                {
                    action(lite);
                    errors.Add(lite.Key(), "");
                }
                catch (Exception e)
                {
                    e.Data["lite"] = lite;
                    e.LogException();
                    errors.Add(lite.Key(), e.Message);
                }
            }
            return errors;
        }


#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        [JsonConverter(typeof(ArgsJsonConverter))]
        public class MultiOperationRequest : BaseOperationRequest
        {
            public string Type { get; set; }
            public Lite<Entity>[] Lites { get; set; }

            public List<PropertySetter>? Setters { get; set; }
        }

        public class PropertySetter
        {
            public string Property;
            public PropertyOperation? Operation;
            public FilterOperation? FilterOperation;
            public object? Value;
            public string? EntityType;
            public List<PropertySetter>? Predicate;
            public List<PropertySetter>? Setters;
        }

#pragma warning restore CS8618 // Non-nullable field is uninitialized.

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

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public class StateCanExecuteRequest
        {
            public string[] OperationKeys { get; set; }
            public Lite<Entity>[] Lites { get; set; }
        }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

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
            JsonSerializer serializer = JsonSerializer.Create(SignumServer.JsonSerializerSettings);

            foreach (var setter in setters)
            {
                var pr = route.Add(setter.Property);

                EntityJsonConverter.AssertCanWrite(pr, entity);

                if (pr.Type.IsMList())
                {
                    var elementPr = pr.Add("Item");
                    var mlist = pr.GetLambdaExpression<ModifiableEntity, IMListPrivate>(false).Compile()(entity);
                    switch (setter.Operation)
                    {
                        case PropertyOperation.AddElement:
                            {
                                var item = (ModifiableEntity)Activator.CreateInstance(elementPr.Type)!;
                                SetSetters(item, setter.Setters!, elementPr);
                                ((IList)mlist).Add(item);
                            }
                            break;
                        case PropertyOperation.ChangeElements:
                            {
                                var predicate = GetPredicate(setter.Predicate!, elementPr, serializer);
                                var toChange = ((IEnumerable<object>)mlist).Where(predicate.Compile()).ToList();
                                foreach (var item in toChange)
                                {
                                    SetSetters((ModifiableEntity)item, setter.Setters!, elementPr);
                                }
                            }
                            break;
                        case PropertyOperation.RemoveElements:
                            {
                                var predicate = GetPredicate(setter.Predicate!, elementPr, serializer);
                                var toRemove = ((IEnumerable<object>)mlist).Where(predicate.Compile()).ToList();
                                foreach (var item in toRemove)
                                {
                                    ((IList)mlist).Remove(item);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
                else if (setter.Operation == PropertyOperation.CreateNewEntiy)
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
                else
                {
                    var value = ConvertObject(setter.Value, pr, serializer);
                    SetProperty(entity, pr, route, value);
                }
            }
        }

        private static void SetProperty(ModifiableEntity entity, PropertyRoute pr, PropertyRoute parentRoute, object? value)
        {
            var subEntity = pr.Parent == parentRoute ? entity :
                        (ModifiableEntity)pr.GetLambdaExpression<object, object>(true, parentRoute).Compile()(entity);

            pr.PropertyInfo!.SetValue(subEntity, value);
        }

        private static object? GetProperty(ModifiableEntity entity, PropertyRoute pr, PropertyRoute parentRoute)
        {
            var subEntity = pr.Parent == parentRoute ? entity :
                        (ModifiableEntity)pr.GetLambdaExpression<object, object>(true, parentRoute).Compile()(entity);

            return pr.PropertyInfo!.GetValue(subEntity);
        }


        static Expression<Func<object, bool>> GetPredicate(List<PropertySetter> predicate, PropertyRoute mainRoute, JsonSerializer serializer)
        {
            var param = Expression.Parameter(typeof(object), "p");

            var body = predicate.Select(p =>
            {
                var pr = mainRoute.Add(p.Property);

                var lambda = pr.GetLambdaExpression<object, object>(true, mainRoute.GetMListItemsRoute());

                var left = Expression.Invoke(lambda, param);
                object? objClean = ConvertObject(p.Value, pr, serializer);

                return (Expression)QueryUtils.GetCompareExpression(p.FilterOperation!.Value, left, Expression.Constant(objClean), inMemory: true);

            }).Aggregate((a, b) => Expression.AndAlso(a, b));

            return Expression.Lambda<Func<object, bool>>(body, param);
        }

        private static object? ConvertObject(object? value, PropertyRoute pr, JsonSerializer serializer)
        {
            var objRaw = value == null ? null :
                            value is JValue jval ? jval.Value :
                            value is JObject jobj ? serializer.Deserialize(new JTokenReader(jobj), pr.Type) :
                            value;

            var objClean = ReflectionTools.ChangeType(objRaw, pr.Type);
            return objClean;
        }
    }
}
