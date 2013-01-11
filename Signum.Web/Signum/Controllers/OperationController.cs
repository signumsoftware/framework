#region usings
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Threading;
using Signum.Services;
using Signum.Utilities;
using Signum.Entities;
using Signum.Web;
using Signum.Engine;
using Signum.Engine.Operations;
using Signum.Engine.Basics;
using Signum.Entities.Basics;
using Signum.Web.Operations;
#endregion

namespace Signum.Web.Controllers
{
    public class OperationController : Controller
    {
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Execute(string operationFullKey, bool isLite, string prefix, string oldPrefix)
        {
            Enum operationKey = OperationsClient.GetOperationKeyAssert(operationFullKey);

            IdentifiableEntity entity = null;
            if (isLite)
            {
                Lite<IdentifiableEntity> lite = this.ExtractLite<IdentifiableEntity>(oldPrefix);
                entity = OperationLogic.ServiceExecuteLite(lite, operationKey);
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity(oldPrefix).UntypedApplyChanges(this.ControllerContext, oldPrefix, true).UntypedValidateGlobal();
                entity = (IdentifiableEntity)context.UntypedValue;

                if (context.GlobalErrors.Any())
                {
                    this.ModelState.FromContext(context);
                    return JsonAction.ModelState(ModelState);
                }

                entity = OperationLogic.ServiceExecute(entity, operationKey);
            }

           return OperationsClient.DefaultExecuteResult(this, entity, prefix);
        }


        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ContextualExecute(string operationFullKey, string oldPrefix)
        {
            Enum operationKey = OperationsClient.GetOperationKeyAssert(operationFullKey);

            RuntimeInfo runtimeInfo = GetRuntimeInfoWithId(oldPrefix, operationKey);

            Lite<IdentifiableEntity> lite = Lite.Create(runtimeInfo.EntityType, runtimeInfo.IdOrNull.Value);
            IdentifiableEntity entity = OperationLogic.ServiceExecuteLite(lite, operationKey);

            return Content("");
        }

    

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(string operationFullKey, string prefix, string oldPrefix)
        {
            Enum operationKey = OperationsClient.GetOperationKeyAssert(operationFullKey);

            RuntimeInfo runtimeInfo = GetRuntimeInfoWithId(oldPrefix, operationKey);

            Lite<IIdentifiable> lite = Lite.Create(runtimeInfo.EntityType, runtimeInfo.IdOrNull.Value);

            OperationLogic.ServiceDelete(lite, MultiEnumLogic<OperationDN>.ToEnum(operationFullKey), null);

            if (Navigator.Manager.QuerySettings.ContainsKey(runtimeInfo.EntityType))
                return JsonAction.Redirect(Navigator.FindRoute(runtimeInfo.EntityType));
            return Content("");
        }

        RuntimeInfo GetRuntimeInfoWithId(string oldPrefix, Enum operationKey)
        {
            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(Request.Form[TypeContextUtilities.Compose(oldPrefix, EntityBaseKeys.RuntimeInfo)]);
            if (!runtimeInfo.IdOrNull.HasValue)
                throw new ArgumentException("Could not create a Lite without an Id to call Operation {0}".Formato(operationKey.ToString()));
            return runtimeInfo;
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ConstructFrom(string operationFullKey, bool isLite, string prefix, string oldPrefix)
        {
            Enum operationKey = OperationsClient.GetOperationKeyAssert(operationFullKey);

            IdentifiableEntity entity = null;
            if (isLite)
            {
                Lite<IdentifiableEntity> lite = this.ExtractLite<IdentifiableEntity>(oldPrefix);
                entity = (IdentifiableEntity)OperationLogic.ServiceConstructFromLite(lite, operationKey);
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity(oldPrefix).UntypedApplyChanges(this.ControllerContext, oldPrefix, true).UntypedValidateGlobal();
                entity = (IdentifiableEntity)context.UntypedValue;

                if (context.GlobalErrors.Any())
                {
                    this.ModelState.FromContext(context);
                    return JsonAction.ModelState(ModelState);
                }

                entity = (IdentifiableEntity)OperationLogic.ServiceConstructFrom(entity, operationKey);
            }

            return OperationsClient.DefaultConstructResult(this, entity, prefix);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ConstructFromMany(string entityType, string operationFullKey, string prefix)
        {
            Enum operationKey = OperationsClient.GetOperationKeyAssert(operationFullKey);

            var keys = Request["keys"];
            if (string.IsNullOrEmpty(keys))
                throw new ArgumentException("Construct from many operation {0} needs source Lite keys".Formato(operationFullKey));
            
            Type type = Navigator.ResolveType(entityType);
            
            List<Lite<IIdentifiable>> sourceEntities = keys.Split(new [] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => (Lite<IIdentifiable>)Lite.Parse(k)).ToList();

            IdentifiableEntity entity = OperationLogic.ServiceConstructFromMany(sourceEntities, type, operationKey);

            return OperationsClient.DefaultConstructResult(this, entity, prefix);
        }
    }
}
