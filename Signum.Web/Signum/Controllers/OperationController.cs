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
using Signum.Entities.Reflection;

namespace Signum.Web.Controllers
{
    public class OperationController : Controller
    {
        [HttpPost, ValidateAntiForgeryToken, ActionSplitter("operationFullKey")]
        public ActionResult Execute()
        {
            OperationSymbol operationSymbol = this.GetOperationKeyAssert();

            Entity entity = null;
            if (this.IsLite())
            {
                Lite<Entity> lite = this.ExtractLite<Entity>();
                entity = OperationLogic.ServiceExecuteLite(lite, operationSymbol);
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(this).UntypedValidate();
                entity = (Entity)context.UntypedValue;

                if (context.HasErrors())
                    return context.ToJsonModelState();

                try
                {
                    entity = OperationLogic.ServiceExecute(entity, operationSymbol);
                }
                catch (IntegrityCheckException e)
                {
                    context.ImportErrors(e.Errors);
                    return context.ToJsonModelState();
                }
            }

           return this.DefaultExecuteResult(entity);
        }

        [HttpPost, ValidateAntiForgeryToken, ActionSplitter("operationFullKey")]
        public ActionResult Delete()
        {
            OperationSymbol operationSymbol = this.GetOperationKeyAssert();

            if (this.IsLite())
            {
                Lite<Entity> lite = this.ExtractLite<Entity>();

                OperationLogic.ServiceDelete(lite, operationSymbol, null);

                return this.DefaultDelete(lite.EntityType);
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(this).UntypedValidate();

                Entity entity = (Entity)context.UntypedValue;

                try
                {
                    OperationLogic.ServiceDelete(entity, operationSymbol, null);
                }
                catch (IntegrityCheckException e)
                {
                    context.ImportErrors(e.Errors);
                    return context.ToJsonModelState();
                }

                return this.DefaultDelete(entity.GetType());
            }
        }

        [HttpPost, ValidateAntiForgeryToken, ActionSplitter("operationFullKey")]
        public ActionResult ConstructFrom()
        {
            OperationSymbol operationSymbol = this.GetOperationKeyAssert();

            Entity entity = null;
            if (this.IsLite())
            {
                Lite<Entity> lite = this.ExtractLite<Entity>();
                entity = OperationLogic.ServiceConstructFromLite(lite, operationSymbol);
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(this).UntypedValidate();

                entity = (Entity)context.UntypedValue;

                if (context.HasErrors())
                    return context.ToJsonModelState();

                try
                {
                    entity = OperationLogic.ServiceConstructFrom(entity, operationSymbol);
                }
                catch (IntegrityCheckException e)
                {
                    context.ImportErrors(e.Errors);
                    return context.ToJsonModelState();
                }
            }

            return this.DefaultConstructResult(entity, operation: operationSymbol);
        }

        [HttpPost, ValidateAntiForgeryToken, ActionSplitter("operationFullKey")]
        public ActionResult ConstructFromMany()
        {
            OperationSymbol operationSymbol = this.GetOperationKeyAssert();

            var lites = this.ParseLiteKeys<Entity>();

            Entity entity = OperationLogic.ServiceConstructFromMany(lites, lites.First().EntityType, operationSymbol);

            return this.DefaultConstructResult(entity, operation: operationSymbol);
        }


        [HttpPost, ValidateAntiForgeryToken, ActionSplitter("operationFullKey")]
        public ActionResult ExecuteMultiple()
        {
            OperationSymbol operationSymbol = this.GetOperationKeyAssert();

            var lites = this.ParseLiteKeys<Entity>();

            foreach (var item in lites)
            {
                OperationLogic.ServiceExecuteLite(item, operationSymbol);
            }

            return null;
        }

        [HttpPost, ValidateAntiForgeryToken, ActionSplitter("operationFullKey")]
        public ActionResult DeleteMultiple()
        {
            OperationSymbol operationSymbol = this.GetOperationKeyAssert();

            var lites = this.ParseLiteKeys<Entity>();

            foreach (var item in lites)
            {
                OperationLogic.ServiceDelete(item, operationSymbol);
            }

            return null;
        }

        [HttpPost, ValidateAntiForgeryToken, ActionSplitter("operationFullKey")]
        public ActionResult ConstructFromMultiple()
        {
            OperationSymbol operationSymbol = this.GetOperationKeyAssert();

            var lites = this.ParseLiteKeys<Entity>();

            foreach (var item in lites)
            {
                OperationLogic.ServiceConstructFromLite(item, operationSymbol);
            }

            return null;
        }
    }
}
