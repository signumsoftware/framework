﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using Signum.React.Facades;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine;
using Signum.Engine.Operations;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Signum.Entities.Reflection;
using Newtonsoft.Json;
using Signum.React.Json;
using Signum.React.Filters;

namespace Signum.React.ApiControllers
{
    public class OperationController : ApiController
    {
        [Route("api/operation/construct"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public EntityPackTS Construct(ConstructOperationRequest request)
        {
            var operation = ParseOperationAssert(request.operationKey);

            var type = TypeLogic.GetType(request.type);

            var entity = OperationLogic.ServiceConstruct(type, operation, request.args);

            return SignumServer.GetEntityPack(entity);
        }

        [Route("api/operation/constructFromEntity"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public EntityPackTS ConstructFromEntity(EntityOperationRequest request)
        {
            var operation = ParseOperationAssert(request.operationKey);

            var entity = OperationLogic.ServiceConstructFrom(request.entity, operation, request.args);

            return SignumServer.GetEntityPack(entity);
        }

        [Route("api/operation/constructFromLite"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public EntityPackTS ConstructFromLite(LiteOperationRequest request)
        {
            var operation = ParseOperationAssert(request.operationKey);

            var entity = OperationLogic.ServiceConstructFromLite(request.lite, operation, request.args);

            return SignumServer.GetEntityPack(entity);
        }

      
        [Route("api/operation/executeEntity"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public EntityPackTS ExecuteEntity(EntityOperationRequest request)
        {
            var operation = ParseOperationAssert(request.operationKey);

            Entity entity;
            try
            {
                entity = OperationLogic.ServiceExecute(request.entity, operation, request.args);
            }
            catch (IntegrityCheckException ex)
            {
                GraphExplorer.SetValidationErrors(GraphExplorer.FromRoot(request.entity), ex);
                this.Validate(request, "request");
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, this.ModelState));
            }

            return SignumServer.GetEntityPack(entity);
        }


        [Route("api/operation/executeLite"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public EntityPackTS ExecuteLite(LiteOperationRequest request)
        {
            var operation = ParseOperationAssert(request.operationKey);

            var entity = OperationLogic.ServiceExecuteLite(request.lite, operation, request.args);

            return SignumServer.GetEntityPack(entity);
        }

        [Route("api/operation/deleteEntity"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public void DeleteEntity(EntityOperationRequest request)
        {
            var operation = ParseOperationAssert(request.operationKey);

            OperationLogic.ServiceDelete(request.entity, operation, request.args);

        }

        [Route("api/operation/deleteLite"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public void DeleteLite(LiteOperationRequest request)
        {
            var operation = ParseOperationAssert(request.operationKey);

            OperationLogic.ServiceDelete(request.lite, operation, request.args);
        }


        public class ConstructOperationRequest
        {
            public string operationKey { get; set; }
            public string type { get; set; }
            [JsonConverter(typeof(ArgsJsonConverter))]
            public object[] args { get; set; }

            public override string ToString() => operationKey;
        }


        public class EntityOperationRequest
        {
            public string operationKey { get; set; }
            public Entity entity { get; set; }
            [JsonConverter(typeof(ArgsJsonConverter))]
            public object[] args { get; set; }

            public override string ToString() => operationKey;
        }

        public class LiteOperationRequest
        {
            public string operationKey { get; set; }
            public Lite<Entity> lite { get; set; }
            [JsonConverter(typeof(ArgsJsonConverter))]
            public object[] args { get; set; }

            public override string ToString() => operationKey;
        }

        [Route("api/operation/constructFromMany"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public EntityPackTS ConstructFromMany(MultiOperationRequest request)
        {
            var operation = ParseOperationAssert(request.operationKey);

            var type = request.type == null ? null : TypeLogic.GetType(request.type);

            var entity = OperationLogic.ServiceConstructFromMany(request.lites, type, operation, request.args);

            return SignumServer.GetEntityPack(entity);
        }

        [Route("api/operation/constructFromMultiple"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public MultiOperationResponse ConstructFromMultiple(MultiOperationRequest request)
        {
            var operation = ParseOperationAssert(request.operationKey);

            var errors = ForeachMultiple(request.lites, lite =>
                OperationLogic.ServiceConstructFromLite(lite, operation, request.args));

            return new MultiOperationResponse { errors = errors };
        }


        [Route("api/operation/executeMultiple"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public MultiOperationResponse ExecuteMultiple(MultiOperationRequest request)
        {
            var operation = ParseOperationAssert(request.operationKey);

            var errors = ForeachMultiple(request.lites, lite =>
                        OperationLogic.ServiceExecuteLite(lite, operation, request.args));

            return new MultiOperationResponse { errors = errors };
        }

        [Route("api/operation/deleteMultiple"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public MultiOperationResponse DeleteMultiple(MultiOperationRequest request)
        {
            var operation = ParseOperationAssert(request.operationKey);

            var errors = ForeachMultiple(request.lites, lite =>
                    OperationLogic.ServiceDelete(lite, operation, request.args));

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

        public class MultiOperationRequest
        {
            public string operationKey { get; set; }
            public string type { get; set; }
            public Lite<Entity>[] lites { get; set; }
            [JsonConverter(typeof(ArgsJsonConverter))]
            public object[] args { get; set; }

            public override string ToString() => operationKey;
        }

        public class MultiOperationResponse
        {
            public Dictionary<string, string> errors { get; set; }
        }

        [Route("api/operation/stateCanExecutes"), HttpPost, ValidateModelFilter]
        public StateCanExecuteResponse StateCanExecutes(StateCanExecuteRequest request)
        {
            var result = OperationLogic.GetContextualCanExecute(request.lites, request.operationKeys.Select(ParseOperationAssert).ToList());
            
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


        public static OperationSymbol ParseOperationAssert(string operationKey)
        {
            var symbol = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

            OperationLogic.AssertOperationAllowed(symbol, inUserInterface: true);

            return symbol;
        }
    }
}