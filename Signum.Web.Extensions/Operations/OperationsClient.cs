#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Engine.Operations;
using Signum.Entities.Operations;
using Signum.Utilities;
using Signum.Entities;
using System.Web;
using Signum.Entities.Basics;
using Signum.Web.Extensions.Properties;
using Signum.Entities.Reflection;
using Signum.Engine.Maps;
using Signum.Engine.Basics;
#endregion

namespace Signum.Web.Operations
{
    public static class OperationsClient
    {
        public static OperationManager Manager { get; private set; }

        public static void Start(OperationManager operationManager, bool contextualMenuInSearchWindow)
        {
            Manager = operationManager;

            Navigator.RegisterArea(typeof(OperationsClient));
            
            var scripts = Navigator.Manager.DefaultScripts();
            scripts.Add("~/Operations/Scripts/SF_Operations.js");
            Navigator.Manager.DefaultScripts += () => scripts;

            Navigator.Manager.SaveProtected += type => OperationLogic.IsSaveProtected(type);

            Navigator.Manager.DefaultSFUrls.Add(url =>
            {
                return new Dictionary<string, string> 
                { 
                    { "operationExecute", url.Action("Execute", "Operation") },
                    { "operationContextual", url.Action("ContextualExecute", "Operation") },
                    { "operationContextualFromMany", url.Action("ContextualExecute", "Process") },
                    { "operationDelete", url.Action("Delete", "Operation") },
                    { "operationConstructFrom", url.Action("ConstructFrom", "Operation") },
                    { "operationConstructFromMany", url.Action("ConstructFromMany", "Operation") },
                };
            });

            ButtonBarEntityHelper.RegisterGlobalButtons(Manager.ButtonBar_GetButtonBarElement);

            Constructor.ConstructorManager.GeneralConstructor += new Func<Type, ModifiableEntity>(Manager.ConstructorManager_GeneralConstructor);
            Constructor.ConstructorManager.VisualGeneralConstructor += new Func<ConstructContext, ActionResult>(Manager.ConstructorManager_VisualGeneralConstructor); 
            
            ContextualItemsHelper.GetContextualItemsForLites += new GetContextualItemDelegate(CreateConstructFromManyGroup);

            if (contextualMenuInSearchWindow)
                OperationsContextualItemsHelper.Start();
        }

        public static void AddSetting(OperationSettings setting)
        {
            Manager.Settings.AddOrThrow(setting.Key, setting, "EntitySettings {0} repeated");
        }

        public static void AddSettings(List<OperationSettings> settings)
        {
            Manager.Settings.AddRange(settings, s => s.Key, s => s, "EntitySettings");
        }


        public static ActionResult DefaultExecuteResult(ControllerBase controller, IdentifiableEntity entity, string prefix)
        {
            if (prefix.HasText())
            {
                TypeContext tc = TypeContextUtilities.UntypedNew(entity, prefix);
                var popupOptions = controller.ControllerContext.HttpContext.Request[ViewDataKeys.OkVisible].HasText() ?
                    (PopupOptionsBase)new PopupViewOptions(tc) :
                    new PopupNavigateOptions(tc);

                return controller.PopupOpen(popupOptions);
            }
            else
            {
                var request = controller.ControllerContext.RequestContext.HttpContext.Request;

                string newUrl = Navigator.NavigateRoute(entity);
                if (request.IsAjaxRequest())
                {
                    if (request.UrlReferrer.AbsolutePath.Contains(newUrl))
                        return Navigator.NormalControl(controller, entity);
                    else
                        return JsonAction.Redirect(newUrl);
                }
                else
                {
                    if (request.UrlReferrer.AbsolutePath.Contains(newUrl))
                        return Navigator.NormalPage(controller, entity);
                    else
                        return new RedirectResult(newUrl);
                }
            }
        }

        public static ActionResult DefaultConstructResult(ControllerBase controller, IdentifiableEntity entity, string prefix)
        {
            if (prefix.HasText())
            {
                TypeContext tc = TypeContextUtilities.UntypedNew(entity, prefix);
                return controller.PopupOpen(new PopupNavigateOptions(tc));
            }
            else //NormalWindow
            {
                var request = controller.ControllerContext.RequestContext.HttpContext.Request;

                if (request.IsAjaxRequest())
                {
                    if (entity.IsNew)
                        return Navigator.NormalControl(controller, entity);
                    else
                        return JsonAction.Redirect(Navigator.NavigateRoute(entity));
                }
                else
                {
                    if (entity.IsNew)
                        return Navigator.NormalPage(controller, entity);
                    else
                        return new RedirectResult(Navigator.NavigateRoute(entity));
                }
            }
        }

        private static ContextualItem CreateConstructFromManyGroup(SelectedItemsMenuContext ctx)
        {
            if (ctx.Lites.IsNullOrEmpty())
                return null;

            if (ctx.Implementations.IsByAll)
                return null;

            List<ContextualItem> operations = GetConstructFromManyOperations(ctx);
            if (operations == null || operations.Count == 0)
                return null;

            HtmlStringBuilder content = new HtmlStringBuilder();
            using (content.Surround(new HtmlTag("ul").Class("sf-search-ctxmenu-constructors")))
            {
                string ctxItemClass = "sf-search-ctxitem";

                content.AddLine(new HtmlTag("li")
                    .Class(ctxItemClass + " sf-search-ctxitem-header")
                    .InnerHtml(new HtmlTag("span").InnerHtml(Signum.Web.Extensions.Properties.Resources.Search_CtxMenuItem_Operations.EncodeHtml())));

                foreach (var operation in operations)
                {
                    content.AddLine(new HtmlTag("li")
                        .Class(ctxItemClass)
                        .InnerHtml(OperationsContextualItemsHelper.IndividualOperationToString(operation)));
                }
            }

            return new ContextualItem
            {
                Id = TypeContextUtilities.Compose(ctx.Prefix, "ctxItemConstructors"),
                Content = content.ToHtml().ToString()
            };
        }

        private static List<ContextualItem> GetConstructFromManyOperations(SelectedItemsMenuContext ctx)
        {
            var contexts = (from t in ctx.Implementations.Types
                            from oi in OperationLogic.ServiceGetOperationInfos(t)
                            where oi.OperationType == OperationType.ConstructorFromMany
                            let os = (ContextualOperationSettings)OperationsClient.Manager.Settings.TryGetC(oi.Key)
                            group Tuple.Create(t, oi, os) by oi.Key into g
                            let context = new ContextualOperationContext
                            {
                                OperationInfo = g.First().Item2,
                                Prefix = ctx.Prefix,
                                QueryName = ctx.QueryName,
                                Entities = ctx.Lites,
                                OperationSettings = g.First().Item3
                            }
                            where string.IsNullOrEmpty(context.OperationInfo.CanExecute)
                                && (context.OperationSettings == null
                                    || (context.OperationSettings != null && (context.OperationSettings.IsVisible == null || (context.OperationSettings.IsVisible != null && context.OperationSettings.IsVisible(context)))))
                            select context
                    );

            return contexts.Select(op => OperationButtonFactory.CreateContextual(op)).ToList();
        }

        internal static Enum GetOperationKeyAssert(string operationFullKey)
        {
            var operationKey = MultiEnumLogic<OperationDN>.ToEnum(operationFullKey);

            OperationLogic.AssertOperationAllowed(operationKey, inUserInterface: true);

            return operationKey;
        }
    }

    public class OperationManager
    {
        public Dictionary<Enum, OperationSettings> Settings = new Dictionary<Enum, OperationSettings>();

        internal ToolBarButton[] ButtonBar_GetButtonBarElement(EntityButtonContext ctx, ModifiableEntity entity)
        {
            IdentifiableEntity ident = entity as IdentifiableEntity;

            if (ident == null)
                return null;

            var list = OperationLogic.ServiceGetEntityOperationInfos(ident);

            var contexts =
                    from oi in list
                    let os = (EntityOperationSettings)Settings.TryGetC(oi.Key)
                    let octx = new EntityOperationContext
                    {
                         Entity = ident,
                         OperationSettings = os,
                         OperationInfo = oi,
                         PartialViewName = ctx.PartialViewName,
                         Prefix = ctx.Prefix
                    }
                    where (os == null || os.IsVisible == null || os.IsVisible(octx))
                    select octx;

            List<ToolBarButton> buttons = contexts
                .Where(oi => oi.OperationInfo.OperationType != OperationType.ConstructorFrom || 
                            (oi.OperationInfo.OperationType == OperationType.ConstructorFrom && oi.OperationSettings != null && !oi.OperationSettings.GroupInMenu))
                .Select(octx => OperationButtonFactory.Create(octx))
                .ToList();

            var constructFroms = contexts.Where(oi => oi.OperationInfo.OperationType == OperationType.ConstructorFrom && 
                            (oi.OperationSettings == null || (oi.OperationSettings != null && oi.OperationSettings.GroupInMenu)));
            if (constructFroms.Any())
            {
                string createText = Resources.Create;
                buttons.Add(new ToolBarMenu
                {
                    Id = "tmConstructors",
                    AltText = createText,
                    Text = createText,
                    DivCssClass = ToolBarButton.DefaultEntityDivCssClass,
                    Items = constructFroms.Select(octx => OperationButtonFactory.Create(octx)).ToList()
                });
            }

            return buttons.ToArray();
        }

        internal ModifiableEntity ConstructorManager_GeneralConstructor(Type type)
        {
            if (!type.IsIIdentifiable())
                return null;

            OperationInfo constructor = OperationLogic.ServiceGetOperationInfos(type).SingleOrDefaultEx(a => a.OperationType == OperationType.Constructor);

            if (constructor == null)
                return null;

            return (ModifiableEntity)OperationLogic.ServiceConstruct(type, constructor.Key);
        }

        internal ActionResult ConstructorManager_VisualGeneralConstructor(ConstructContext ctx)
        {
            var count = OperationLogic.ServiceGetOperationInfos(ctx.Type).Count(a => a.OperationType == OperationType.Constructor);

            if (count == 0 || count == 1)
                return null;

            throw new NotImplementedException();  //show chooser
        }
    }
}
