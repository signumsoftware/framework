using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
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
            var entityType = TypeLogic.GetType(request.type);

            var entity = OperationLogic.ServiceConstruct(entityType, request.GetOperationSymbol(entityType), request.args);

            return entity == null ? null : SignumServer.GetEntityPack(entity);
        }

        [HttpPost("api/operation/constructFromEntity"), ProfilerActionSplitter]
        public EntityPackTS? ConstructFromEntity([Required, FromBody] EntityOperationRequest request)
        {
            var entity = OperationLogic.ServiceConstructFrom(request.entity, request.GetOperationSymbol(request.entity.GetType()), request.args);

            return entity == null ? null : SignumServer.GetEntityPack(entity);
        }

        [HttpPost("api/operation/constructFromLite"), ProfilerActionSplitter]
        public EntityPackTS? ConstructFromLite([Required, FromBody] LiteOperationRequest request)
        {
            var entity = OperationLogic.ServiceConstructFromLite(request.lite, request.GetOperationSymbol(request.lite.EntityType), request.args);
            return entity == null ? null : SignumServer.GetEntityPack(entity);
        }


        [HttpPost("api/operation/executeEntity"), ProfilerActionSplitter]
        public ActionResult<EntityPackTS> ExecuteEntity([Required, FromBody] EntityOperationRequest request)
        {
            Entity entity;
            try
            {
                entity = OperationLogic.ServiceExecute(request.entity, request.GetOperationSymbol(request.entity.GetType()), request.args);
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
            var entity = OperationLogic.ServiceExecuteLite(request.lite, request.GetOperationSymbol(request.lite.EntityType), request.args);

            return SignumServer.GetEntityPack(entity);
        }

        [HttpPost("api/operation/deleteEntity"), ProfilerActionSplitter]
        public void DeleteEntity([Required, FromBody] EntityOperationRequest request)
        {
            OperationLogic.ServiceDelete(request.entity, request.GetOperationSymbol(request.entity.GetType()), request.args);
        }

        [HttpPost("api/operation/deleteLite"), ProfilerActionSplitter]
        public void DeleteLite([Required, FromBody] LiteOperationRequest request)
        {
            OperationLogic.ServiceDelete(request.lite, request.GetOperationSymbol(request.lite.EntityType), request.args);
        }


#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        [JsonConverter(typeof(ArgsJsonConverter))]
        public class ConstructOperationRequest : BaseOperationRequest
        {
            public string type { get; set; }
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
            public string operationKey { get; set; }

            public object?[]? args { get; set; }

            public OperationSymbol GetOperationSymbol(Type entityType) => ParseOperationAssert(this.operationKey, entityType, this.args);

            public static OperationSymbol ParseOperationAssert(string operationKey, Type entityType, object?[]? args = null)
            {
                var symbol = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

                OperationLogic.AssertOperationAllowed(symbol, entityType, inUserInterface: true);

                return symbol;
            }

            public override string ToString() => operationKey;
        }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

        [HttpPost("api/operation/constructFromMany"), ProfilerActionSplitter]
        public EntityPackTS ConstructFromMany([Required, FromBody] MultiOperationRequest request)
        {
            var type = request.lites.Select(l => l.EntityType).Distinct().Only() ?? TypeLogic.GetType(request.type);

            var entity = OperationLogic.ServiceConstructFromMany(request.lites, type, request.GetOperationSymbol(type), request.args);

            return SignumServer.GetEntityPack(entity);
        }

        [HttpPost("api/operation/constructFromMultiple"), ProfilerActionSplitter]
        public MultiOperationResponse ConstructFromMultiple([Required, FromBody] MultiOperationRequest request)
        {
            if (request.setters.HasItems())
            {
                var errors = ForeachMultiple(request.lites, lite =>
                {
                    var entity = lite.RetrieveAndForget();

                    MultiSetter.SetSetters(entity, request.setters, PropertyRoute.Root(entity.GetType()));

                    OperationLogic.ServiceConstructFrom(entity, request.GetOperationSymbol(entity.GetType()), request.args);
                });

                return new MultiOperationResponse(errors);
            }
            else
            {
                var errors = ForeachMultiple(request.lites, lite =>
                            OperationLogic.ServiceConstructFromLite(lite, request.GetOperationSymbol(lite.EntityType), request.args));

                return new MultiOperationResponse(errors);
            }
        }


        [HttpPost("api/operation/executeMultiple"), ProfilerActionSplitter]
        public MultiOperationResponse ExecuteMultiple([Required, FromBody] MultiOperationRequest request)
        {
            if (request.setters.HasItems())
            {
                var errors = ForeachMultiple(request.lites, lite =>
                {
                    var entity = lite.RetrieveAndForget();

                    MultiSetter.SetSetters(entity, request.setters, PropertyRoute.Root(entity.GetType()));

                    OperationLogic.ServiceExecute(entity, request.GetOperationSymbol(entity.GetType()), request.args);
                });

                return new MultiOperationResponse(errors);
            }
            else
            {
                var errors = ForeachMultiple(request.lites, lite =>
                            OperationLogic.ServiceExecuteLite(lite, request.GetOperationSymbol(lite.EntityType), request.args));

                return new MultiOperationResponse(errors);
            }
        }


        [HttpPost("api/operation/deleteMultiple"), ProfilerActionSplitter]
        public MultiOperationResponse DeleteMultiple([Required, FromBody] MultiOperationRequest request)
        {
            if (request.setters.HasItems())
            {
                var errors = ForeachMultiple(request.lites, lite =>
                {
                    var entity = lite.RetrieveAndForget();

                    MultiSetter.SetSetters(entity, request.setters, PropertyRoute.Root(entity.GetType()));

                    OperationLogic.ServiceDelete(entity, request.GetOperationSymbol(entity.GetType()), request.args);
                });

                return new MultiOperationResponse(errors);
            }
            else
            {
                var errors = ForeachMultiple(request.lites, lite =>
                            OperationLogic.ServiceDelete(lite, request.GetOperationSymbol(lite.EntityType), request.args));

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
            public string type { get; set; }
            public Lite<Entity>[] lites { get; set; }

            public List<PropertySetter>? setters { get; set; }
        }

        public class PropertySetter
        {
            public string property;
            public object? value;
            public ListAction? listAction;
            public NewEntityAction? newEntity;
        }

        public class ListAction
        {
            public ListActionType type;
            public List<PropertySetter>? predicate;
            public List<PropertySetter>? setters;
        }

        public class NewEntityAction
        {
            public List<PropertySetter> setters;
        }

        public enum ListActionType
        {
            Add,
            Change,
            Remove
        }

#pragma warning restore CS8618 // Non-nullable field is uninitialized.

        public class MultiOperationResponse
        {
            public MultiOperationResponse(Dictionary<string, string> errors)
            {
                this.errors = errors;
            }

            public Dictionary<string, string> errors { get; set; }
        }

        [HttpPost("api/operation/stateCanExecutes"), ValidateModelFilter]
        public StateCanExecuteResponse StateCanExecutes([Required, FromBody]StateCanExecuteRequest request)
        {
            var types = request.lites.Select(a => a.EntityType).ToHashSet();

            var operationSymbols = request.operationKeys
                .Select(operationKey => types.Select(t => BaseOperationRequest.ParseOperationAssert(operationKey, t)).Distinct().SingleEx())
                .ToList();

            var result = OperationLogic.GetContextualCanExecute(request.lites, operationSymbols);
            var anyReadonly = AnyReadonly.GetInvocationListTyped().Any(f => f(request.lites));

            return new StateCanExecuteResponse(result.SelectDictionary(a => a.Key, v => v))
            {
                anyReadonly = anyReadonly
            };
        }


        public static Func<Lite<Entity>[], bool>? AnyReadonly; 

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public class StateCanExecuteRequest
        {
            public string[] operationKeys { get; set; }
            public Lite<Entity>[] lites { get; set; }
        }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

        public class StateCanExecuteResponse
        {
            public StateCanExecuteResponse(Dictionary<string, string> canExecutes)
            {
                this.canExecutes = canExecutes;
            }

            public bool anyReadonly;
            public Dictionary<string, string> canExecutes { get; set; }
        }
    }

    internal static class MultiSetter
    {
        public static void SetSetters(ModifiableEntity entity, List<PropertySetter> setters, PropertyRoute route)
        {
            JsonSerializer serializer = JsonSerializer.Create(SignumServer.JsonSerializerSettings);

            foreach (var setter in setters)
            {
                var pr = route.Add(setter.property);

                EntityJsonConverter.AssertCanWrite(pr, entity);

                if (pr.Type.IsMList())
                {
                    if (setter.listAction == null)
                        throw new InvalidOperationException("listAction not set");

                    var elementPr = pr.Add("Item");
                    var mlist = pr.GetLambdaExpression<ModifiableEntity, IMListPrivate>(false).Compile()(entity);
                    switch (setter.listAction.type)
                    {
                        case ListActionType.Add:
                            {
                                var item = (ModifiableEntity)Activator.CreateInstance(pr.Type.ElementType()!)!;
                                SetSetters(item, setter.listAction.setters!, pr);
                                ((IList)mlist).Add(item);
                            }
                            break;
                        case ListActionType.Change:
                            {
                                var predicate = GetPredicate(setter.listAction.predicate!, elementPr, serializer);
                                var toChange = ((IEnumerable<object>)mlist).Where(predicate.Compile()).ToList();
                                foreach (var item in toChange)
                                {
                                    SetSetters((ModifiableEntity)item, setter.listAction.setters!, elementPr);
                                }
                            }
                            break;
                        case ListActionType.Remove:
                            {
                                var predicate = GetPredicate(setter.listAction.predicate!, elementPr, serializer);
                                var toRemove = ((IEnumerable<object>)mlist).Where(predicate.Compile()).ToList();
                                foreach (var item in toRemove)
                                {
                                    ((IList)mlist).Add(item);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
                else if (setter.newEntity != null)
                {
                    var item = (ModifiableEntity)Activator.CreateInstance(pr.Type.ElementType()!)!;
                    SetSetters(item, setter.newEntity.setters, pr);
                    SetProperty(entity, pr, route, item);
                }
                else
                {
                    var value = ConvertObject(setter.value, pr, serializer);
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

        static MethodInfo miEquals = ReflectionTools.GetMethodInfo(() => object.Equals(null, null));

        static Expression<Func<object, bool>> GetPredicate(List<PropertySetter> predicate, PropertyRoute mainRoute, JsonSerializer serializer)
        {
            var param = Expression.Parameter(typeof(object), "p");

            var body = predicate.Select(p =>
            {
                var pr = mainRoute.Add(p.property);

                var lambda = pr.GetLambdaExpression<object, object>(true, mainRoute.GetMListItemsRoute());

                var left = Expression.Invoke(lambda, param);
                object? objClean = ConvertObject(p.value, pr, serializer);

                return (Expression)Expression.Call(null, miEquals, left, Expression.Constant(objClean));

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
