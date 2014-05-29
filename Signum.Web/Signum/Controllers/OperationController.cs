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
            OperationSymbol operationSymbol = this.GetOperationKeyAssert();

            IdentifiableEntity entity = null;
            if (this.IsLite())
            {
                Lite<IdentifiableEntity> lite = this.ExtractLite<IdentifiableEntity>();
                entity = OperationLogic.ServiceExecuteLite(lite, operationSymbol);
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(this).UntypedValidateGlobal();
                entity = (IdentifiableEntity)context.UntypedValue;

                if (context.HasErrors())
                    return context.ToJsonModelState();

                entity = OperationLogic.ServiceExecute(entity, operationSymbol);
            }

           return this.DefaultExecuteResult(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete()
        {
            OperationSymbol operationSymbol = this.GetOperationKeyAssert();

            if (this.IsLite())
            {
                Lite<IdentifiableEntity> lite = this.ExtractLite<IdentifiableEntity>();

                OperationLogic.ServiceDelete(lite, operationSymbol, null);

                return this.DefaultDelete(lite.EntityType);
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(this).UntypedValidateGlobal();
                IdentifiableEntity entity = (IdentifiableEntity)context.UntypedValue;

                OperationLogic.ServiceDelete(entity, operationSymbol, null);

                return this.DefaultDelete(entity.GetType());
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ConstructFrom()
        {
            OperationSymbol operationSymbol = this.GetOperationKeyAssert();

            IdentifiableEntity entity = null;
            if (this.IsLite())
            {
                Lite<IdentifiableEntity> lite = this.ExtractLite<IdentifiableEntity>();
                entity = OperationLogic.ServiceConstructFromLite(lite, operationSymbol);
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(this).UntypedValidateGlobal();
                entity = (IdentifiableEntity)context.UntypedValue;

                if (context.HasErrors())
                    return context.ToJsonModelState();

                entity = OperationLogic.ServiceConstructFrom(entity, operationSymbol);
            }

            return this.DefaultConstructResult(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ConstructFromMany()
        {
            OperationSymbol operationKey = this.GetOperationKeyAssert();

            var lites = this.ParseLiteKeys<IdentifiableEntity>();

            IdentifiableEntity entity = OperationLogic.ServiceConstructFromMany(lites, lites.First().EntityType, operationKey);

            return this.DefaultConstructResult(entity);
        }
    }
}
