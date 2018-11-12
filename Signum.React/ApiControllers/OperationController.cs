using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.React.Facades;
using Signum.React.Filters;
using Signum.React.Json;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;

namespace Signum.React.ApiControllers
{
    [ValidateModelFilter]
    public class OperationController : Controller
    {
        [HttpPost("api/operation/construct"), ValidateModelFilter, ProfilerActionSplitter]
        public EntityPackTS Construct([Required, FromBody]ConstructOperationRequest request)
        {
            var entityType = TypeLogic.GetType(request.type);

            var entity = OperationLogic.ServiceConstruct(entityType, request.GetOperationSymbol(entityType), request.args);

            return entity == null ? null : SignumServer.GetEntityPack(entity);
        }

        [HttpPost("api/operation/constructFromEntity"), ProfilerActionSplitter]
        public EntityPackTS ConstructFromEntity([Required, FromBody]EntityOperationRequest request)
        {
            var entity = OperationLogic.ServiceConstructFrom(request.entity, request.GetOperationSymbol(request.entity.GetType()), request.args);

            return entity == null ? null: SignumServer.GetEntityPack(entity);
        }

        [HttpPost("api/operation/constructFromLite"), ProfilerActionSplitter]
        public EntityPackTS ConstructFromLite([Required, FromBody]LiteOperationRequest request)
        {
            var entity = OperationLogic.ServiceConstructFromLite(request.lite, request.GetOperationSymbol(request.lite.EntityType), request.args);
            return entity == null ? null: SignumServer.GetEntityPack(entity);
        }


        [HttpPost("api/operation/executeEntity"), ProfilerActionSplitter]
        public ActionResult<EntityPackTS> ExecuteEntity([Required, FromBody]EntityOperationRequest request)
        {
            Entity entity;
            try
            {
                entity = OperationLogic.ServiceExecute(request.entity, request.GetOperationSymbol(request.entity.GetType()), request.args);
            }
            catch (IntegrityCheckException ex)
            {
                GraphExplorer.SetValidationErrors(GraphExplorer.FromRoot(request.entity), ex);
                this.TryValidateModel(request, "request");
                return BadRequest(this.ModelState);
            }

            return SignumServer.GetEntityPack(entity);
        }


        [HttpPost("api/operation/executeLite"), ProfilerActionSplitter]
        public EntityPackTS ExecuteLite([Required, FromBody]LiteOperationRequest request)
        {
            var entity = OperationLogic.ServiceExecuteLite(request.lite, request.GetOperationSymbol(request.lite.EntityType), request.args);

            return SignumServer.GetEntityPack(entity);
        }

        [HttpPost("api/operation/deleteEntity"), ProfilerActionSplitter]
        public void DeleteEntity([Required, FromBody]EntityOperationRequest request)
        {
            OperationLogic.ServiceDelete(request.entity, request.GetOperationSymbol(request.entity.GetType()), request.args);
        }

        [HttpPost("api/operation/deleteLite"), ProfilerActionSplitter]
        public void DeleteLite([Required, FromBody]LiteOperationRequest request)
        {
            OperationLogic.ServiceDelete(request.lite, request.GetOperationSymbol(request.lite.EntityType), request.args);
        }


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

            public object[] args { get; set; }

            public OperationSymbol GetOperationSymbol(Type entityType) => ParseOperationAssert(this.operationKey, entityType, this.args);

            public static OperationSymbol ParseOperationAssert(string operationKey, Type entityType, object[] args = null)
            {
                var symbol = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

                OperationLogic.AssertOperationAllowed(symbol, entityType, inUserInterface: true);

                return symbol;
            }

            public override string ToString() => operationKey;
        }

        [HttpPost("api/operation/constructFromMany"), ProfilerActionSplitter]
        public EntityPackTS ConstructFromMany([Required, FromBody]MultiOperationRequest request)
        {
            var type = request.lites.Select(l => l.EntityType).Distinct().Only() ?? TypeLogic.GetType(request.type);

            var entity = OperationLogic.ServiceConstructFromMany(request.lites, type, request.GetOperationSymbol(type), request.args);

            return SignumServer.GetEntityPack(entity);
        }

        [HttpPost("api/operation/constructFromMultiple"), ProfilerActionSplitter]
        public MultiOperationResponse ConstructFromMultiple([Required, FromBody]MultiOperationRequest request)
        {
            var errors = ForeachMultiple(request.lites, lite =>
                OperationLogic.ServiceConstructFromLite(lite, request.GetOperationSymbol(lite.EntityType), request.args));

            return new MultiOperationResponse { errors = errors };
        }


        [HttpPost("api/operation/executeMultiple"), ProfilerActionSplitter]
        public MultiOperationResponse ExecuteMultiple([Required, FromBody]MultiOperationRequest request)
        {
            var errors = ForeachMultiple(request.lites, lite =>
                        OperationLogic.ServiceExecuteLite(lite, request.GetOperationSymbol(lite.EntityType), request.args));

            return new MultiOperationResponse { errors = errors };
        }

        [HttpPost("api/operation/deleteMultiple"), ProfilerActionSplitter]
        public MultiOperationResponse DeleteMultiple([Required, FromBody]MultiOperationRequest request)
        {
            var errors = ForeachMultiple(request.lites, lite =>
                    OperationLogic.ServiceDelete(lite, request.GetOperationSymbol(lite.EntityType), request.args));

            return new MultiOperationResponse { errors = errors };
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


        [JsonConverter(typeof(ArgsJsonConverter))]
        public class MultiOperationRequest : BaseOperationRequest
        {
            public string type { get; set; }
            public Lite<Entity>[] lites { get; set; }
        }

        public class MultiOperationResponse
        {
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

            return new StateCanExecuteResponse { canExecutes = result.SelectDictionary(a => a.Key, v => v) };
        }

        public class StateCanExecuteRequest
        {
            public string[] operationKeys { get; set; }
            public Lite<Entity>[] lites { get; set; }
        }

        public class StateCanExecuteResponse
        {
            public Dictionary<string, string> canExecutes { get; set; }
        }


    }
}