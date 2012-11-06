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
using Signum.Entities.Operations;
using Signum.Engine.Basics;
using Signum.Web.Extensions.Properties;
#endregion

namespace Signum.Web.Operations
{
    public class OperationController : Controller
    {
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Execute(string operationFullKey, bool isLite, string prefix, string oldPrefix)
        {
            IdentifiableEntity entity = null;
            if (isLite)
            {
                Lite<IdentifiableEntity> lite = this.ExtractLite<IdentifiableEntity>(oldPrefix);
                entity = OperationLogic.ServiceExecuteLite(lite, MultiEnumLogic<OperationDN>.ToEnum(operationFullKey));
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

                entity = OperationLogic.ServiceExecute(entity, MultiEnumLogic<OperationDN>.ToEnum(operationFullKey));
            }

           return OperationsClient.DefaultExecuteResult(this, entity, prefix);
        }


        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ContextualExecute(string operationFullKey, string oldPrefix)
        {
            IdentifiableEntity entity = null;
            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(Request.Form[TypeContextUtilities.Compose(oldPrefix, EntityBaseKeys.RuntimeInfo)]);
            if (!runtimeInfo.IdOrNull.HasValue)
                throw new ArgumentException("Could not create a Lite without an Id to call Operation {0}".Formato(operationFullKey));

            Lite lite = Lite.Create(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value);
            entity = OperationLogic.ServiceExecuteLite(lite, MultiEnumLogic<OperationDN>.ToEnum(operationFullKey));

            return Content("");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(string operationFullKey, string prefix, string oldPrefix)
        {
            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(Request.Form[TypeContextUtilities.Compose(oldPrefix, EntityBaseKeys.RuntimeInfo)]);
            if (!runtimeInfo.IdOrNull.HasValue)
                throw new ArgumentException("Could not create a Lite without an Id to call Operation {0}".Formato(operationFullKey));

            Lite lite = Lite.Create(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value);
            OperationLogic.ServiceDelete(lite, MultiEnumLogic<OperationDN>.ToEnum(operationFullKey), null);

            if (Navigator.Manager.QuerySettings.ContainsKey(runtimeInfo.RuntimeType))
                return JsonAction.Redirect(Navigator.FindRoute(runtimeInfo.RuntimeType));
            return Content("");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ConstructFrom(string operationFullKey, bool isLite, string prefix, string oldPrefix)
        {
            IdentifiableEntity entity = null;
            if (isLite)
            {
                Lite<IdentifiableEntity> lite = this.ExtractLite<IdentifiableEntity>(oldPrefix);
                entity = (IdentifiableEntity)OperationLogic.ServiceConstructFromLite(lite, MultiEnumLogic<OperationDN>.ToEnum(operationFullKey));
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

                entity = (IdentifiableEntity)OperationLogic.ServiceConstructFrom(entity, MultiEnumLogic<OperationDN>.ToEnum(operationFullKey));
            }

            return OperationsClient.DefaultConstructResult(this, entity, prefix);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ConstructFromMany(string runtimeType, List<int> ids, string operationFullKey, string prefix)
        {
            Type type = Navigator.ResolveType(runtimeType);

            if (ids == null || ids.Count == 0)
                throw new ArgumentException("Construct from many operation {0} needs source Ids".Formato(operationFullKey));

            List<Lite> sourceEntities = ids.Select(idstr => Lite.Create(type, idstr)).ToList();
            
            IdentifiableEntity entity = OperationLogic.ServiceConstructFromMany(sourceEntities, type, MultiEnumLogic<OperationDN>.ToEnum(operationFullKey));

            return OperationsClient.DefaultConstructResult(this, entity, prefix);
        }
    }
}
