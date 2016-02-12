using System;
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

namespace Signum.React.ApiControllers
{
    public class OperationController : ApiController
    {
        [Route("api/operation/construct"), HttpPost, ValidateModel]
        public EntityPackTS Construct(ConstructOperationRequest request)
        {
            var operation = SymbolLogic<OperationSymbol>.ToSymbol(request.operationKey);

            var type = TypeLogic.GetType(request.type);

            var entity = OperationLogic.ServiceConstruct(type, operation, request.args);

            return EntityServer.GetEntityPack(entity);
        }

        [Route("api/operation/constructFromEntity"), HttpPost, ValidateModel]
        public EntityPackTS ConstructFromEntity(EntityOperationRequest request)
        {
            var operation = SymbolLogic<OperationSymbol>.ToSymbol(request.operationKey);

            var entity = OperationLogic.ServiceConstructFrom(request.entity, operation, request.args);

            return EntityServer.GetEntityPack(entity);
        }

        [Route("api/operation/constructFromLite"), HttpPost, ValidateModel]
        public EntityPackTS ConstructFromLite(LiteOperationRequest request)
        {
            var operation = SymbolLogic<OperationSymbol>.ToSymbol(request.operationKey);

            var entity = OperationLogic.ServiceConstructFromLite(request.lite, operation, request.args);

            return EntityServer.GetEntityPack(entity);
        }

      
        [Route("api/operation/executeEntity"), HttpPost, ValidateModel]
        public EntityPackTS ExecuteEntity(EntityOperationRequest request)
        {
            var operation = SymbolLogic<OperationSymbol>.ToSymbol(request.operationKey);

            var entity = OperationLogic.ServiceExecute(request.entity, operation, request.args);

            return EntityServer.GetEntityPack(entity);
        }


        [Route("api/operation/executeLite"), HttpPost, ValidateModel]
        public EntityPackTS ExecuteLite(LiteOperationRequest request)
        {
            var operation = SymbolLogic<OperationSymbol>.ToSymbol(request.operationKey);

            var entity = OperationLogic.ServiceExecuteLite(request.lite, operation, request.args);

            return EntityServer.GetEntityPack(entity);
        }

        [Route("api/operation/deleteEntity"), HttpPost, ValidateModel]
        public void DeleteEntity(EntityOperationRequest request)
        {
            var operation = SymbolLogic<OperationSymbol>.ToSymbol(request.operationKey);

            OperationLogic.ServiceDelete(request.entity, operation, request.args);

        }

        [Route("api/operation/deleteLite"), HttpPost, ValidateModel]
        public void DeleteLite(LiteOperationRequest request)
        {
            var operation = SymbolLogic<OperationSymbol>.ToSymbol(request.operationKey);

            OperationLogic.ServiceDelete(request.lite, operation, request.args);
        }

        public class ConstructOperationRequest
        {
            public string operationKey { get; set; }
            public string type { get; set; }
            public object[] args { get; set; }
        }


        public class EntityOperationRequest
        {
            public string operationKey { get; set; }
            public Entity entity { get; set; }
            public object[] args { get; set; }
        }

        public class LiteOperationRequest
        {
            public string operationKey { get; set; }
            public Lite<Entity> lite { get; set; }
            public object[] args { get; set; }
        }

        [Route("api/operation/constructFromMany"), HttpPost, ValidateModel]
        public EntityPackTS ConstructFromMany(MultiOperationRequest request)
        {
            var operation = SymbolLogic<OperationSymbol>.ToSymbol(request.operationKey);

            var type = TypeLogic.GetType(request.type);

            var entity = OperationLogic.ServiceConstructFromMany(request.lites, type, operation, request.args);

            return EntityServer.GetEntityPack(entity);
        }

        [Route("api/operation/constructFromMultiple"), HttpPost, ValidateModel]
        public MultiOperationResponse ConstructFromMultiple(MultiOperationRequest request)
        {
            var operation = SymbolLogic<OperationSymbol>.ToSymbol(request.operationKey);

            var errors = ForeachMultiple(request.lites, lite =>
                OperationLogic.ServiceConstructFromLite(lite, operation, request.args));

            return new MultiOperationResponse { errors = errors };
        }

        [Route("api/operation/executeMultiple"), HttpPost, ValidateModel]
        public MultiOperationResponse ExecuteMultiple(MultiOperationRequest request)
        {
            var operation = SymbolLogic<OperationSymbol>.ToSymbol(request.operationKey);

            var errors = ForeachMultiple(request.lites, lite =>
                        OperationLogic.ServiceExecuteLite(lite, operation, request.args));

            return new MultiOperationResponse { errors = errors };
        }

        [Route("api/operation/deleteMultiple"), HttpPost, ValidateModel]
        public MultiOperationResponse DeleteMultiple(MultiOperationRequest request)
        {
            var operation = SymbolLogic<OperationSymbol>.ToSymbol(request.operationKey);

            var errors = ForeachMultiple(request.lites, lite =>
                    OperationLogic.ServiceDelete(lite, operation, request.args));

            return new MultiOperationResponse { errors = errors };
        }

        static Dictionary<string, string> ForeachMultiple(IEnumerable<Lite<Entity>> lites, Action<Lite<Entity>> action)
        {
            Dictionary<string, string> errors = new Dictionary<string, string>();
            foreach (var lite in lites)
            {
                try
                {
                    action(lite);
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
            public object[] args { get; set; }
        }

        public class MultiOperationResponse
        {
            public Dictionary<string, string> errors { get; set; }
        }
    }

    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (!actionContext.ModelState.IsValid)
            {
                actionContext.Response = actionContext.Request.CreateErrorResponse(
                    HttpStatusCode.BadRequest, actionContext.ModelState);
            }
        }
    }
}