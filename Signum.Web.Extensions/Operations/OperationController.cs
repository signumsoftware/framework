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
        public ActionResult OperationExecute(string sfOperationFullKey, bool isLite, string prefix, string sfOldPrefix, string sfOnOk, string sfOnCancel)
        {
            IdentifiableEntity entity = null;
            if (isLite)
            {
                RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(Request.Form[TypeContextUtilities.Compose(sfOldPrefix, EntityBaseKeys.RuntimeInfo)]);
                if (!runtimeInfo.IdOrNull.HasValue)
                    throw new ArgumentException("Could not create a Lite without an Id to call Operation {0}".Formato(sfOperationFullKey));

                Lite lite = Lite.Create(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value);
                entity = OperationLogic.ServiceExecuteLite(lite, EnumLogic<OperationDN>.ToEnum(sfOperationFullKey));

            }
            else
            {
                MappingContext context = this.UntypedExtractEntity(sfOldPrefix).UntypedApplyChanges(this.ControllerContext, sfOldPrefix, true).UntypedValidateGlobal();
                entity = (IdentifiableEntity)context.UntypedValue;

                if (context.GlobalErrors.Any())
                {
                    this.ModelState.FromContext(context);
                    return Navigator.ModelState(ModelState);
                }

                entity = OperationLogic.ServiceExecute(entity, EnumLogic<OperationDN>.ToEnum(sfOperationFullKey));

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
        public ActionResult ContextualExecute(string sfOperationFullKey, string sfOldPrefix)
        {
            IdentifiableEntity entity = null;
            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(Request.Form[TypeContextUtilities.Compose(sfOldPrefix, EntityBaseKeys.RuntimeInfo)]);
            if (!runtimeInfo.IdOrNull.HasValue)
                throw new ArgumentException("Could not create a Lite without an Id to call Operation {0}".Formato(sfOperationFullKey));

            Lite lite = Lite.Create(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value);
            entity = OperationLogic.ServiceExecuteLite(lite, EnumLogic<OperationDN>.ToEnum(sfOperationFullKey));

            return Content("");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DeleteExecute(string sfOperationFullKey, string prefix, string sfOldPrefix, string sfOnOk, string sfOnCancel)
        {
            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(Request.Form[TypeContextUtilities.Compose(sfOldPrefix, EntityBaseKeys.RuntimeInfo)]);
            if (!runtimeInfo.IdOrNull.HasValue)
                throw new ArgumentException("Could not create a Lite without an Id to call Operation {0}".Formato(sfOperationFullKey));

            Lite lite = Lite.Create(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value);
            OperationLogic.ServiceDelete(lite, EnumLogic<OperationDN>.ToEnum(sfOperationFullKey), null);

            if (Navigator.Manager.QuerySettings.ContainsKey(runtimeInfo.RuntimeType))
                return Navigator.RedirectUrl(Navigator.FindRoute(runtimeInfo.RuntimeType));
            return Content("");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ConstructFromExecute(string sfOperationFullKey, bool isLite, string prefix, string sfOldPrefix, string sfOnOk, string sfOnCancel)
        {
            IdentifiableEntity entity = null;
            if (isLite)
            {
                RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(Request.Form[TypeContextUtilities.Compose(sfOldPrefix ?? "", EntityBaseKeys.RuntimeInfo)]);

                if (!runtimeInfo.IdOrNull.HasValue)
                    throw new ArgumentException("Could not create a Lite without an Id to call Operation {0}".Formato(sfOperationFullKey));

                Lite lite = Lite.Create(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value);
                entity = (IdentifiableEntity)OperationLogic.ServiceConstructFromLite(lite, EnumLogic<OperationDN>.ToEnum(sfOperationFullKey));
            }
            else
            {
                MappingContext context = this.UntypedExtractEntity(sfOldPrefix).UntypedApplyChanges(this.ControllerContext, sfOldPrefix, true).UntypedValidateGlobal();
                entity = (IdentifiableEntity)context.UntypedValue;

                if (context.GlobalErrors.Any())
                {
                    this.ModelState.FromContext(context);
                    return Navigator.ModelState(ModelState);
                }

                entity = (IdentifiableEntity)OperationLogic.ServiceConstructFrom(entity, EnumLogic<OperationDN>.ToEnum(sfOperationFullKey));
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
        public ActionResult ConstructFromManyExecute(string sfRuntimeType, List<int> sfIds, string sfOperationFullKey, string prefix, string sfOnOk, string sfOnCancel)
        {
            Type type = Navigator.ResolveType(sfRuntimeType);

            if (sfIds == null || sfIds.Count == 0)
                throw new ArgumentException("Construct from many operation {0} needs source Ids".Formato(sfOperationFullKey));

            List<Lite> sourceEntities = sfIds.Select(idstr => Lite.Create(type, idstr)).ToList();
            
            IdentifiableEntity entity = OperationLogic.ServiceConstructFromMany(sourceEntities, type, EnumLogic<OperationDN>.ToEnum(sfOperationFullKey));

            ViewData[ViewDataKeys.WriteSFInfo] = true;
            return Navigator.PopupView(this, entity, prefix);
        }
    }
}
