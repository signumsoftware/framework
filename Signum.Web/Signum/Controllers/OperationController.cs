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
        public ActionResult Execute(string operationFullKey, bool isLite, string prefix)
        {
            Enum operationKey = OperationsClient.GetOperationKeyAssert(operationFullKey);

            IdentifiableEntity entity = null;
            if (isLite)
            {
                Lite<IdentifiableEntity> lite = this.ExtractLite<IdentifiableEntity>(prefix);
                entity = OperationLogic.ExecuteLite<IdentifiableEntity>(lite, operationKey);
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity(prefix).UntypedApplyChanges(this.ControllerContext, prefix, true).UntypedValidateGlobal();
                entity = (IdentifiableEntity)context.UntypedValue;

                if (context.GlobalErrors.Any())
                {
                    this.ModelState.FromContext(context);
                    return JsonAction.ModelState(ModelState);
                }

                entity = OperationLogic.Execute<IdentifiableEntity>(entity, operationKey);
            }

           return OperationsClient.DefaultExecuteResult(this, entity, prefix);
        }


        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ContextualExecute(string operationFullKey, string liteKeys)
        {
            Enum operationKey = OperationsClient.GetOperationKeyAssert(operationFullKey);

            var lite = Navigator.ParseLiteKeys<IdentifiableEntity>(liteKeys).Single();

            IdentifiableEntity entity = OperationLogic.ExecuteLite(lite, operationKey);

            return Content("");
        }


        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(string operationFullKey, string prefix)
        {
            Enum operationKey = OperationsClient.GetOperationKeyAssert(operationFullKey);

            Lite<IdentifiableEntity> lite = this.ExtractLite<IdentifiableEntity>(prefix);

            OperationLogic.Delete(lite, MultiEnumLogic<OperationDN>.ToEnum(operationFullKey), null);
            
            return JsonAction.Redirect(Navigator.FindRoute(lite.EntityType));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ContextualDelete(string operationFullKey, string liteKeys)
        {
            Enum operationKey = OperationsClient.GetOperationKeyAssert(operationFullKey);

            var lite = Navigator.ParseLiteKeys<IdentifiableEntity>(liteKeys).Single();

            OperationLogic.Delete(lite, operationKey);

            return Content("");
        }


        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ConstructFrom(string operationFullKey, bool isLite, string prefix, string newPrefix)
        {
            Enum operationKey = OperationsClient.GetOperationKeyAssert(operationFullKey);

            IdentifiableEntity entity = null;
            if (isLite)
            {
                Lite<IdentifiableEntity> lite = this.ExtractLite<IdentifiableEntity>(prefix);
                entity = OperationLogic.ConstructFromLite<IdentifiableEntity>(lite, operationKey);
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity(prefix).UntypedApplyChanges(this.ControllerContext, prefix, true).UntypedValidateGlobal();
                entity = (IdentifiableEntity)context.UntypedValue;

                if (context.GlobalErrors.Any())
                {
                    this.ModelState.FromContext(context);
                    return JsonAction.ModelState(ModelState);
                }

                entity = OperationLogic.ConstructFrom<IdentifiableEntity>(entity, operationKey);
            }

            return OperationsClient.DefaultConstructResult(this, entity, newPrefix);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ContextualConstructFrom(string operationFullKey, string liteKeys, string newPrefix)
        {
            Enum operationKey = OperationsClient.GetOperationKeyAssert(operationFullKey);

            var lite = Navigator.ParseLiteKeys<IdentifiableEntity>(liteKeys).Single();

            var entity = lite.ConstructFromLite<IdentifiableEntity>(operationKey);

          return OperationsClient.DefaultConstructResult(this, entity, newPrefix);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ConstructFromMany(string operationFullKey, string liteKeys, string newPrefix)
        {
            Enum operationKey = OperationsClient.GetOperationKeyAssert(operationFullKey);

            var lites = Navigator.ParseLiteKeys<IdentifiableEntity>(liteKeys);

            IdentifiableEntity entity = OperationLogic.ServiceConstructFromMany(lites, lites.Select(a => a.EntityType).First(), operationKey);

            return OperationsClient.DefaultConstructResult(this, entity, newPrefix);
        }
    }
}
