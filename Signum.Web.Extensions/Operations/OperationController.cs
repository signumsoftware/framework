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
    [HandleException, AuthenticationRequired]
    public class OperationController : Controller
    {
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult OperationExecute(string operationFullKey, bool isLite, string prefix, string oldPrefix)
        {
            IdentifiableEntity entity = null;
            if (isLite)
            {
                RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(Request.Form[TypeContextUtilities.Compose(oldPrefix, EntityBaseKeys.RuntimeInfo)]);
                if (!runtimeInfo.IdOrNull.HasValue)
                    throw new ArgumentException("Could not create a Lite without an Id to call Operation {0}".Formato(operationFullKey));

                Lite lite = Lite.Create(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value);
                entity = OperationLogic.ServiceExecuteLite(lite, EnumLogic<OperationDN>.ToEnum(operationFullKey));

            }
            else
            {
                MappingContext context = this.UntypedExtractEntity(oldPrefix).UntypedApplyChanges(this.ControllerContext, oldPrefix, true).UntypedValidateGlobal();
                entity = (IdentifiableEntity)context.UntypedValue;

                if (context.GlobalErrors.Any())
                {
                    this.ModelState.FromContext(context);
                    return Navigator.ModelState(ModelState);
                }

                entity = OperationLogic.ServiceExecute(entity, EnumLogic<OperationDN>.ToEnum(operationFullKey));

                if (this.IsReactive())
                    Session[this.TabID()] = entity;
            }

            if (prefix.HasText())
            {
                ViewData[ViewDataKeys.WriteSFInfo] = true;
                return Navigator.PopupView(this, entity, prefix);
            }
            else //NormalWindow
            {
                if (Request.IsAjaxRequest())
                {
                    if (entity.IsNew)
                        return Navigator.NormalControl(this, entity);

                    string newUrl = Navigator.ViewRoute(entity.GetType(), entity.Id);
                    if (HttpContext.Request.UrlReferrer.AbsolutePath.Contains(newUrl))
                        return Navigator.NormalControl(this, entity);
                    else
                        return Navigator.RedirectUrl(newUrl);
                }
                else
                    return Navigator.View(this, entity);
            }
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ContextualExecute(string operationFullKey, string oldPrefix)
        {
            IdentifiableEntity entity = null;
            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(Request.Form[TypeContextUtilities.Compose(oldPrefix, EntityBaseKeys.RuntimeInfo)]);
            if (!runtimeInfo.IdOrNull.HasValue)
                throw new ArgumentException("Could not create a Lite without an Id to call Operation {0}".Formato(operationFullKey));

            Lite lite = Lite.Create(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value);
            entity = OperationLogic.ServiceExecuteLite(lite, EnumLogic<OperationDN>.ToEnum(operationFullKey));

            return Content("");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DeleteExecute(string operationFullKey, string prefix, string oldPrefix)
        {
            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(Request.Form[TypeContextUtilities.Compose(oldPrefix, EntityBaseKeys.RuntimeInfo)]);
            if (!runtimeInfo.IdOrNull.HasValue)
                throw new ArgumentException("Could not create a Lite without an Id to call Operation {0}".Formato(operationFullKey));

            Lite lite = Lite.Create(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value);
            OperationLogic.ServiceDelete(lite, EnumLogic<OperationDN>.ToEnum(operationFullKey), null);

            if (Navigator.Manager.QuerySettings.ContainsKey(runtimeInfo.RuntimeType))
                return Navigator.RedirectUrl(Navigator.FindRoute(runtimeInfo.RuntimeType));
            return Content("");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ConstructFromExecute(string operationFullKey, bool isLite, string prefix, string oldPrefix)
        {
            IdentifiableEntity entity = null;
            if (isLite)
            {
                RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(Request.Form[TypeContextUtilities.Compose(oldPrefix ?? "", EntityBaseKeys.RuntimeInfo)]);

                if (!runtimeInfo.IdOrNull.HasValue)
                    throw new ArgumentException("Could not create a Lite without an Id to call Operation {0}".Formato(operationFullKey));

                Lite lite = Lite.Create(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value);
                entity = (IdentifiableEntity)OperationLogic.ServiceConstructFromLite(lite, EnumLogic<OperationDN>.ToEnum(operationFullKey));
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity(oldPrefix).UntypedApplyChanges(this.ControllerContext, oldPrefix, true).UntypedValidateGlobal();
                entity = (IdentifiableEntity)context.UntypedValue;

                if (context.GlobalErrors.Any())
                {
                    this.ModelState.FromContext(context);
                    return Navigator.ModelState(ModelState);
                }

                entity = (IdentifiableEntity)OperationLogic.ServiceConstructFrom(entity, EnumLogic<OperationDN>.ToEnum(operationFullKey));
            }

            if (prefix.HasText())
            {
                ViewData[ViewDataKeys.WriteSFInfo] = true;
                return Navigator.PopupView(this, entity, prefix);
            }
            else //NormalWindow
            {
                if (Request.IsAjaxRequest())
                {
                    if (entity.IsNew)
                        return Navigator.NormalControl(this, entity);

                    string newUrl = Navigator.ViewRoute(entity.GetType(), entity.Id);
                    if (HttpContext.Request.UrlReferrer.AbsolutePath.Contains(newUrl))
                        return Navigator.NormalControl(this, entity);
                    else
                        return Navigator.RedirectUrl(newUrl);
                }
                else
                    return Navigator.View(this, entity);
            }
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ConstructFromManyExecute(string runtimeType, List<int> ids, string operationFullKey, string prefix)
        {
            Type type = Navigator.ResolveType(runtimeType);

            if (ids == null || ids.Count == 0)
                throw new ArgumentException("Construct from many operation {0} needs source Ids".Formato(operationFullKey));

            List<Lite> sourceEntities = ids.Select(idstr => Lite.Create(type, idstr)).ToList();
            
            IdentifiableEntity entity = OperationLogic.ServiceConstructFromMany(sourceEntities, type, EnumLogic<OperationDN>.ToEnum(operationFullKey));

            ViewData[ViewDataKeys.WriteSFInfo] = true;
            return Navigator.PopupView(this, entity, prefix);
        }
    }
}
