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
        [Route("api/operation/executeEntity"), HttpPost, ValidateModel]
        public EntityPackTS ExecuteEntity(EntityOperationRequest request)
        {
            var operation = SymbolLogic<OperationSymbol>.ToSymbol(request.operationKey);

            var entity = OperationLogic.ServiceExecute(request.entity, operation, request.args);

            return EntitesServer.GetEntityPack(entity);
        }

        public class EntityOperationRequest
        {
            public string operationKey;
            public Entity entity;
            public object[] args;
        }

        public class LiteOperationRequest
        {
            public string operationKey;
            public Lite<Entity> entity;
            public object[] args;
        }

        public class MultiOperationRequest
        {
            public string operationKey;
            public Lite<Entity>[] entity;
            public object[] args;
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