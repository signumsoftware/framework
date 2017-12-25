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
            Entity entity = null;
            if (this.IsLite())
            {
                Lite<Entity> lite = this.ExtractLite<Entity>();
                entity = OperationLogic.ServiceExecuteLite(lite, this.GetOperationKeyAssert(lite.EntityType));
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(this).UntypedValidate();
                entity = (Entity)context.UntypedValue;

                if (context.HasErrors())
                    return context.ToJsonModelState();

                try
                {
                    entity = OperationLogic.ServiceExecute(entity, this.GetOperationKeyAssert(entity.GetType()));
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
            if (this.IsLite())
            {
                Lite<Entity> lite = this.ExtractLite<Entity>();

                OperationLogic.ServiceDelete(lite, this.GetOperationKeyAssert(lite.EntityType), null);

                return this.DefaultDelete(lite.EntityType);
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(this).UntypedValidate();

                Entity entity = (Entity)context.UntypedValue;

                try
                {
                    OperationLogic.ServiceDelete(entity, this.GetOperationKeyAssert(entity.GetType()), null);
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
            OperationSymbol operationSymbol;
            Entity entity = null;
            if (this.IsLite())
            {
                Lite<Entity> lite = this.ExtractLite<Entity>();
                entity = OperationLogic.ServiceConstructFromLite(lite, operationSymbol = this.GetOperationKeyAssert(lite.EntityType));
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(this).UntypedValidate();

                entity = (Entity)context.UntypedValue;

                if (context.HasErrors())
                    return context.ToJsonModelState();

                try
                {
                    entity = OperationLogic.ServiceConstructFrom(entity, operationSymbol = this.GetOperationKeyAssert(entity.GetType()));
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

            var lites = this.ParseLiteKeys<Entity>();
            OperationSymbol operationSymbol = lites.Select(a => a.EntityType).Distinct().Select(type => this.GetOperationKeyAssert(type)).Distinct().SingleEx();

            Entity entity = OperationLogic.ServiceConstructFromMany(lites, lites.First().EntityType, operationSymbol);

            return this.DefaultConstructResult(entity, operation: operationSymbol);
        }


        [HttpPost, ValidateAntiForgeryToken, ActionSplitter("operationFullKey")]
        public ActionResult ExecuteMultiple()
        {
            var lites = this.ParseLiteKeys<Entity>();
            OperationSymbol operationSymbol = lites.Select(a => a.EntityType).Distinct().Select(type => this.GetOperationKeyAssert(type)).Distinct().SingleEx();
            

            foreach (var item in lites)
            {
                try
                {
                    OperationLogic.ServiceExecuteLite(item, operationSymbol);
                }
                catch (Exception ex)
                {
                	ex.LogException();
                }
            }

            return null;
        }

        [HttpPost, ValidateAntiForgeryToken, ActionSplitter("operationFullKey")]
        public ActionResult DeleteMultiple()
        {
            var lites = this.ParseLiteKeys<Entity>();
            OperationSymbol operationSymbol = lites.Select(a => a.EntityType).Distinct().Select(type => this.GetOperationKeyAssert(type)).Distinct().SingleEx();
            

            foreach (var item in lites)
            {
             	try
                {
                	OperationLogic.ServiceDelete(item, operationSymbol);
                }
                catch (Exception ex)
                {
                	ex.LogException();
                }
            }

            return null;
        }

        [HttpPost, ValidateAntiForgeryToken, ActionSplitter("operationFullKey")]
        public ActionResult ConstructFromMultiple()
        {
            var lites = this.ParseLiteKeys<Entity>();
            OperationSymbol operationSymbol = lites.Select(a => a.EntityType).Distinct().Select(type => this.GetOperationKeyAssert(type)).Distinct().SingleEx();


            foreach (var item in lites)
            {
            	try
                {
                	OperationLogic.ServiceConstructFromLite(item, operationSymbol);
                }
                catch (Exception ex)
                {
                	ex.LogException();
                }
            }

            return null;
        }
    }
}
