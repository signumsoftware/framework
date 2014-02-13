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
        public ActionResult Execute()
        {
            Enum operationKey = this.GetOperationKeyAssert();

            IdentifiableEntity entity = null;
            if (this.IsLite())
            {
                Lite<IdentifiableEntity> lite = this.ExtractLite<IdentifiableEntity>();
                entity = OperationLogic.ExecuteLite<IdentifiableEntity>(lite, operationKey);
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(this.ControllerContext, admin: true).UntypedValidateGlobal();
                entity = (IdentifiableEntity)context.UntypedValue;

                if (context.GlobalErrors.Any())
                {
                    this.ModelState.FromContext(context);
                    return JsonAction.ModelState(ModelState);
                }

                entity = OperationLogic.Execute<IdentifiableEntity>(entity, operationKey);
            }

           return this.DefaultExecuteResult(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete()
        {
            Enum operationKey = this.GetOperationKeyAssert();

            if (this.IsLite())
            {
                Lite<IdentifiableEntity> lite = this.ExtractLite<IdentifiableEntity>();

                OperationLogic.Delete(lite, operationKey, null);

                return this.DefaultDelete(lite.EntityType);
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(this.ControllerContext, admin: true).UntypedValidateGlobal();
                IdentifiableEntity entity = (IdentifiableEntity)context.UntypedValue;

                OperationLogic.Delete(entity, operationKey, null);

                return this.DefaultDelete(entity.GetType());
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ConstructFrom()
        {
            Enum operationKey = this.GetOperationKeyAssert();

            IdentifiableEntity entity = null;
            if (this.IsLite())
            {
                Lite<IdentifiableEntity> lite = this.ExtractLite<IdentifiableEntity>();
                entity = OperationLogic.ConstructFromLite<IdentifiableEntity>(lite, operationKey);
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(this.ControllerContext, admin: true).UntypedValidateGlobal();
                entity = (IdentifiableEntity)context.UntypedValue;

                if (context.GlobalErrors.Any())
                {
                    this.ModelState.FromContext(context);
                    return JsonAction.ModelState(ModelState);
                }

                entity = OperationLogic.ConstructFrom<IdentifiableEntity>(entity, operationKey);
            }

            return this.DefaultConstructResult(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ConstructFromMany()
        {
            Enum operationKey = this.GetOperationKeyAssert();

            var lites = this.ParseLiteKeys<IdentifiableEntity>();

            IdentifiableEntity entity = OperationLogic.ServiceConstructFromMany(lites, lites.First().EntityType, operationKey);

            return this.DefaultConstructResult(entity);
        }
    }
}
