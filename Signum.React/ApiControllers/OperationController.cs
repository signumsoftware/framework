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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Signum.React.ApiControllers
{
    public class OperationController : ApiController
    {
        [Route("api/operation/construct"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public EntityPackTS Construct(ConstructOperationRequest request)
        {
            var type = TypeLogic.GetType(request.type);

            var entity = OperationLogic.ServiceConstruct(type, request.operarionSymbol, request.args);

            return SignumServer.GetEntityPack(entity);
        }

        [Route("api/operation/constructFromEntity"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public EntityPackTS ConstructFromEntity(EntityOperationRequest request)
        {
            var entity = OperationLogic.ServiceConstructFrom(request.entity, request.operarionSymbol, request.args);

            return SignumServer.GetEntityPack(entity);
        }

        [Route("api/operation/constructFromLite"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public EntityPackTS ConstructFromLite(LiteOperationRequest request)
        {
            var entity = OperationLogic.ServiceConstructFromLite(request.lite, request.operarionSymbol, request.args);

            return SignumServer.GetEntityPack(entity);
        }


        [Route("api/operation/executeEntity"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public EntityPackTS ExecuteEntity(EntityOperationRequest request)
        {
            Entity entity;
            try
            {
                entity = OperationLogic.ServiceExecute(request.entity, request.operarionSymbol, request.args);
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
            var entity = OperationLogic.ServiceExecuteLite(request.lite, request.operarionSymbol, request.args);

            return SignumServer.GetEntityPack(entity);
        }

        [Route("api/operation/deleteEntity"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public void DeleteEntity(EntityOperationRequest request)
        {

            OperationLogic.ServiceDelete(request.entity, request.operarionSymbol, request.args);

        }

        [Route("api/operation/deleteLite"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public void DeleteLite(LiteOperationRequest request)
        {
            OperationLogic.ServiceDelete(request.lite, request.operarionSymbol, request.args);
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

            public OperationSymbol operarionSymbol => ParseOperationAssert(this.operationKey);
            public static OperationSymbol ParseOperationAssert(string operationKey)
            {
                var symbol = SymbolLogic<OperationSymbol>.ToSymbol(operationKey);

                OperationLogic.AssertOperationAllowed(symbol, inUserInterface: true);

                return symbol;
            }

            public override string ToString() => operationKey;
        }

        [Route("api/operation/constructFromMany"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public EntityPackTS ConstructFromMany(MultiOperationRequest request)
        {
            var type = request.type == null ? null : TypeLogic.GetType(request.type);

            var entity = OperationLogic.ServiceConstructFromMany(request.lites, type, request.operarionSymbol, request.args);

            return SignumServer.GetEntityPack(entity);
        }

        [Route("api/operation/constructFromMultiple"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public MultiOperationResponse ConstructFromMultiple(MultiOperationRequest request)
        {
            var errors = ForeachMultiple(request.lites, lite =>
                OperationLogic.ServiceConstructFromLite(lite, request.operarionSymbol, request.args));

            return new MultiOperationResponse { errors = errors };
        }


        [Route("api/operation/executeMultiple"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public MultiOperationResponse ExecuteMultiple(MultiOperationRequest request)
        {
            var errors = ForeachMultiple(request.lites, lite =>
                        OperationLogic.ServiceExecuteLite(lite, request.operarionSymbol, request.args));

            return new MultiOperationResponse { errors = errors };
        }

        [Route("api/operation/deleteMultiple"), HttpPost, ValidateModelFilter, ProfilerActionSplitter]
        public MultiOperationResponse DeleteMultiple(MultiOperationRequest request)
        {
            var errors = ForeachMultiple(request.lites, lite =>
                    OperationLogic.ServiceDelete(lite, request.operarionSymbol, request.args));

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

        [Route("api/operation/stateCanExecutes"), HttpPost, ValidateModelFilter]
        public StateCanExecuteResponse StateCanExecutes(StateCanExecuteRequest request)
        {
            var result = OperationLogic.GetContextualCanExecute(request.lites, request.operationKeys.Select(BaseOperationRequest.ParseOperationAssert).ToList());

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