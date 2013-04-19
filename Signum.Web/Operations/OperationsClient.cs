#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Engine.Operations;
using Signum.Utilities;
using Signum.Entities;
using System.Web;
using Signum.Entities.Basics;
using Signum.Entities.Reflection;
using Signum.Engine.Maps;
using Signum.Engine.Basics;
using Signum.Engine;
using Signum.Utilities.ExpressionTrees;
using System.Collections.Concurrent;
#endregion

namespace Signum.Web.Operations
{
    public static class OperationsClient
    {
        public static OperationManager Manager { get; private set; }

        public static void Start(OperationManager operationManager, bool contextualMenuInSearchWindow)
        {
            Manager = operationManager;

            Navigator.Manager.DefaultSFUrls.Add(url =>
            {
                return new Dictionary<string, string> 
                { 
                    { "operationExecute", url.Action("Execute", "Operation") },
                    { "operationContextual", url.Action("ContextualExecute", "Operation") },
                    { "operationDelete", url.Action("Delete", "Operation") },
                    { "operationConstructFrom", url.Action("ConstructFrom", "Operation") },
                    { "operationConstructFromMany", url.Action("ConstructFromMany", "Operation") },
                };
            });

            ButtonBarEntityHelper.RegisterGlobalButtons(Manager.ButtonBar_GetButtonBarElement);

            Constructor.ConstructorManager.GeneralConstructor += Manager.ConstructorManager_GeneralConstructor;
            Constructor.ConstructorManager.VisualGeneralConstructor += Manager.ConstructorManager_VisualGeneralConstructor;

            ContextualItemsHelper.GetContextualItemsForLites += Manager.ContextualItemsHelper_GetConstructorFromManyMenuItems;
            ContextualItemsHelper.GetContextualItemsForLites += Manager.ContextualItemsHelper_GetEntityOperationMenuItem;
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

        public static Enum GetOperationKeyAssert(string operationFullKey)
        {
            var operationKey = MultiEnumLogic<OperationDN>.ToEnum(operationFullKey);

            OperationLogic.AssertOperationAllowed(operationKey, inUserInterface: true);

            return operationKey;
        }
    }

    public class OperationManager
    {
        public Dictionary<Enum, OperationSettings> Settings = new Dictionary<Enum, OperationSettings>();

        public T GetSettings<T>(Enum key)
            where T : OperationSettings
        {
            OperationSettings settings = Settings.TryGetC(key);
            if (settings != null)
            {
                var result = settings as T;

                if (result == null)
                    throw new InvalidOperationException("{0}({1}) should be a {2}".Formato(settings.GetType().TypeName(), OperationDN.UniqueKey(key), typeof(T).TypeName()));

                return result;
            }

            return null;
        }

        ConcurrentDictionary<Type, List<OperationInfo>> operationInfoCache = new ConcurrentDictionary<Type, List<OperationInfo>>();
        public IEnumerable<OperationInfo> OperationInfos(Type entityType)
        {
            var result = operationInfoCache.GetOrAdd(entityType, OperationLogic.GetAllOperationInfos);

            return result.Where(oi => OperationLogic.OperationAllowed(oi.Key, true));
        }

        #region Execute ToolBarButton
        public virtual ToolBarButton[] ButtonBar_GetButtonBarElement(EntityButtonContext ctx, ModifiableEntity entity)
        {
            IdentifiableEntity ident = entity as IdentifiableEntity;

            if (ident == null)
                return null;

            Type type = ident.GetType();

            var operations = (from oi in OperationInfos(ident.GetType())
                              where oi.IsEntityOperation && (oi.AllowsNew.Value || !ident.IsNew)
                              let os = GetSettings<EntityOperationSettings>(oi.Key)
                              let eoc = new EntityOperationContext
                              {
                                  Entity = (IdentifiableEntity)entity,
                                  OperationInfo = oi,
                                  ViewButtons = ctx.ViewButtons,
                                  ShowOperations = ctx.ShowOperations,
                                  PartialViewName = ctx.PartialViewName,
                                  Prefix = ctx.Prefix,
                                  OperationSettings = os,
                              }
                              where (os != null && os.IsVisible != null) ? os.IsVisible(eoc) : ctx.ShowOperations
                              select eoc).ToList();

            if (operations.Any(eoc => eoc.OperationInfo.HasCanExecute == true))
            {
                Dictionary<Enum, string> canExecutes = OperationLogic.ServiceCanExecute(ident);
                foreach (var eoc in operations)
                {
                    var ce = canExecutes.TryGetC(eoc.OperationInfo.Key);
                    if (ce != null && ce.HasText())
                        eoc.CanExecute = ce;
                }
            }

            List<ToolBarButton> buttons = new List<ToolBarButton>();
            Dictionary<EntityOperationGroup, ToolBarMenu> groups = new Dictionary<EntityOperationGroup,ToolBarMenu>();

            foreach (var eoc in operations)
            {
                //if (eoc.OperationInfo.OperationType == OperationType.ConstructorFrom &&
                //   (eoc.OperationSettings == null || !eoc.OperationSettings.AvoidMoveToSearchControl))
                //{
                //    if(EntityOperationToolBarButton.MoveToSearchControls(eoc))
                //        continue; 
                //}

                EntityOperationGroup group = GetDefaultGroup(eoc);

                if(group != null)
                {
                    var cm = groups.GetOrCreate(group, () =>
                    {
                        var tbm = new ToolBarMenu
                        {
                            Id = group == EntityOperationGroup.Create ? "tmConstructors" : "",
                            AltText = group.Description(),
                            Text = group.Description(),
                            DivCssClass = " ".CombineIfNotEmpty(ToolBarButton.DefaultEntityDivCssClass, group.CssClass),
                            Items = new List<ToolBarButton>(),
                        };

                        buttons.Add(tbm);

                        return tbm;
                    });

                   cm.Items.Add(CreateToolBarButton(eoc, group));
                }
                else
                {
                    buttons.Add(CreateToolBarButton(eoc, null));
                }
            }

            return buttons.OrderBy(a=>a is ToolBarMenu).ToArray();
        }

        private EntityOperationGroup GetDefaultGroup(EntityOperationContext eoc)
        {
            if (eoc.OperationSettings != null && eoc.OperationSettings.Group != null)
                return eoc.OperationSettings.Group == EntityOperationGroup.None ? null : eoc.OperationSettings.Group;

            if (eoc.OperationInfo.OperationType == OperationType.ConstructorFrom)
                return EntityOperationGroup.Create;

            return null;
        }

        protected internal virtual ToolBarButton CreateToolBarButton(EntityOperationContext ctx, EntityOperationGroup group)
        {
            return new ToolBarButton
            {
                Id = MultiEnumDN.UniqueKey(ctx.OperationInfo.Key),

                DivCssClass = " ".CombineIfNotEmpty(
                    ToolBarButton.DefaultEntityDivCssClass,
                    EntityOperationSettings.CssClass(ctx.OperationInfo.Key)),

                AltText = ctx.CanExecute,
                Enabled = ctx.CanExecute == null,

                Text = ctx.OperationSettings.TryCC(o => o.Text) ??  (group == null ? ctx.OperationInfo.Key.NiceToString() : group.SimplifyName(ctx.OperationInfo.Key.NiceToString())),
                OnClick = ((ctx.OperationSettings != null && ctx.OperationSettings.OnClick != null) ? ctx.OperationSettings.OnClick(ctx) : DefaultClick(ctx)).ToJS(),
            };
        }


        protected internal virtual JsInstruction DefaultClick(EntityOperationContext ctx)
        {
            switch (ctx.OperationInfo.OperationType)
            {
                case OperationType.Execute:
                    return new JsOperationExecutor(ctx.Options()).validateAndAjax();
                case OperationType.Delete:
                    return new JsOperationDelete(ctx.Options()).confirmAndAjax(ctx.Entity);
                case OperationType.ConstructorFrom:
                    return new JsOperationConstructorFrom(ctx.Options()).validateAndAjax();
                default:
                    throw new InvalidOperationException("Invalid Operation Type '{0}' in the construction of the operation '{1}'".Formato(ctx.OperationInfo.OperationType.ToString(), MultiEnumDN.UniqueKey(ctx.OperationInfo.Key)));
            }
        }
        #endregion

        #region Constructor
        protected internal virtual ModifiableEntity ConstructorManager_GeneralConstructor(Type type)
        {
            if (!type.IsIIdentifiable())
                return null;

            OperationInfo constructor = OperationInfos(type).SingleOrDefaultEx(a => a.OperationType == OperationType.Constructor);

            if (constructor == null)
                return null;

            return OperationLogic.Construct(type, constructor.Key);
        }

        protected internal virtual ActionResult ConstructorManager_VisualGeneralConstructor(ConstructContext ctx)
        {
            var count = OperationInfos(ctx.Type).Count(a => a.OperationType == OperationType.Constructor);

            if (count == 0 || count == 1)
                return null;

            throw new NotImplementedException();  //show chooser
        }

        #endregion
        
        public virtual ContextualItem ContextualItemsHelper_GetConstructorFromManyMenuItems(SelectedItemsMenuContext ctx)
        {
            if (ctx.Lites.IsNullOrEmpty())
                return null;

            var types = ctx.Lites.Select(a => a.EntityType).Distinct().ToList();

            List<ContextualItem> operations =
                (from t in types
                 from oi in OperationInfos(t)
                 where oi.OperationType == OperationType.ConstructorFromMany
                 group new { t, oi } by oi.Key into g
                 let os = GetSettings<ContextualOperationSettings>(g.Key)
                 let coc = new ContextualOperationContext
                 {
                     Entities = ctx.Lites,
                     OperationSettings = os,
                     OperationInfo = g.First().oi,
                     CanExecute = OperationDN.NotDefinedFor(g.Key, types.Except(g.Select(a => a.t)))
                 }
                 where os == null || os.IsVisible == null || os.IsVisible(coc)
                 select CreateContextual(coc)).ToList();

            if (operations.IsEmpty())
                return null;

            HtmlStringBuilder content = new HtmlStringBuilder();
            using (content.Surround(new HtmlTag("ul").Class("sf-search-ctxmenu-constructors")))
            {
                string ctxItemClass = "sf-search-ctxitem";

                content.AddLine(new HtmlTag("li")
                    .Class(ctxItemClass + " sf-search-ctxitem-header")
                    .InnerHtml(new HtmlTag("span").InnerHtml(SearchMessage.Search_CtxMenuItem_Operations.NiceToString().EncodeHtml())));

                foreach (var operation in operations)
                {
                    content.AddLine(new HtmlTag("li")
                        .Class(ctxItemClass)
                        .InnerHtml(IndividualOperationToString(operation)));
                }
            }

            return new ContextualItem
            {
                Id = TypeContextUtilities.Compose(ctx.Prefix, "ctxItemConstructors"),
                Content = content.ToHtml().ToString()
            };
        }


        public virtual ContextualItem ContextualItemsHelper_GetEntityOperationMenuItem(SelectedItemsMenuContext ctx)
        {
            if (ctx.Lites.IsNullOrEmpty() || ctx.Lites.Count > 1)
                return null;

            List<ContextualOperationContext> context =
                (from oi in OperationInfos(ctx.Lites.Single().EntityType)
                 where oi.IsEntityOperation
                 let os = GetSettings<EntityOperationSettings>(oi.Key)
                 let coc = new ContextualOperationContext
                 {
                     Entities = ctx.Lites,
                     QueryName = ctx.QueryName,
                     OperationSettings = os == null ? null : os.Contextual,
                     OperationInfo = oi,
                     Prefix = ctx.Prefix
                 }
                 where os == null ? oi.Lite == true :
                       os.Contextual.IsVisible == null ? (oi.Lite == true && os.IsVisible == null && (os.OnClick == null || os.Contextual.OnClick != null)) :
                       os.Contextual.IsVisible(coc)
                 select coc).ToList();

            if (context.IsEmpty())
                return null;

            if (context.Any(eomi => eomi.OperationInfo.HasCanExecute == true))
            {
                Dictionary<Enum, string> canExecutes = OperationLogic.ServiceCanExecute(Database.Retrieve(ctx.Lites.Single()));
                foreach (var coc in context)
                {
                    var ce = canExecutes.TryGetC(coc.OperationInfo.Key);
                    if (ce != null)
                        coc.CanExecute = ce;
                }
            }

            List<ContextualItem> buttons = context.Select(coc => CreateContextual(coc)).ToList();

            HtmlStringBuilder content = new HtmlStringBuilder();
            using (content.Surround(new HtmlTag("ul").Class("sf-search-ctxmenu-operations")))
            {
                string ctxItemClass = "sf-search-ctxitem";

                content.AddLine(new HtmlTag("li")
                    .Class(ctxItemClass + " sf-search-ctxitem-header")
                    .InnerHtml(
                        new HtmlTag("span").InnerHtml(SearchMessage.Search_CtxMenuItem_Operations.NiceToString().EncodeHtml()))
                    );

                foreach (var operation in buttons)
                {
                    content.AddLine(new HtmlTag("li")
                        .Class(ctxItemClass)
                        .InnerHtml(IndividualOperationToString(operation)));
                }
            }

            return new ContextualItem
            {
                Id = TypeContextUtilities.Compose(ctx.Prefix, "ctxItemOperations"),
                Content = content.ToHtml().ToString()
            };
        }


        public virtual MvcHtmlString IndividualOperationToString(ContextualItem oci)
        {
            if (oci.Enabled)
                oci.HtmlProps.Add("onclick", oci.OnClick);

            return new HtmlTag("a", oci.Id)
                        .Attrs(oci.HtmlProps)
                        .Attr("title", oci.AltText ?? "")
                        .Class("sf-operation-ctxitem" + ((!oci.Enabled || string.IsNullOrEmpty(oci.OnClick)) ? " sf-disabled" : ""))
                        .SetInnerText(oci.Text)
                        .ToHtml();
        }

        public virtual ContextualItem CreateContextual(ContextualOperationContext ctx)
        {
            return new ContextualItem
            {
                Id = MultiEnumDN.UniqueKey(ctx.OperationInfo.Key),

                DivCssClass = " ".CombineIfNotEmpty(
                    ToolBarButton.DefaultEntityDivCssClass,
                    EntityOperationSettings.CssClass(ctx.OperationInfo.Key)),

                AltText = ctx.CanExecute,
                Enabled = ctx.CanExecute == null,

                Text = ctx.OperationSettings.TryCC(o => o.Text) ?? ctx.OperationInfo.Key.NiceToString(),
                OnClick = (ctx.OperationSettings != null && ctx.OperationSettings.OnClick != null) ? ctx.OperationSettings.OnClick(ctx).ToJS() :
                        DefaultClick(ctx).ToJS()
            };
        }

        protected virtual JsInstruction DefaultClick(ContextualOperationContext ctx)
        {
            switch (ctx.OperationInfo.OperationType)
            {
                case OperationType.Execute:
                    return new JsOperationExecutor(ctx.Options()).ContextualExecute();
                case OperationType.Delete:
                    return new JsOperationDelete(ctx.Options()).ContextualDelete(ctx.Entities);
                case OperationType.ConstructorFrom:
                    return new JsOperationConstructorFrom(ctx.Options()).ContextualConstruct();
                case OperationType.ConstructorFromMany:
                    return new JsOperationConstructorFromMany(ctx.Options()).ajaxSelected(Js.NewPrefix(ctx.Prefix), JsOpSuccess.DefaultDispatcher);
                default:
                    throw new InvalidOperationException("Invalid Operation Type '{0}' in the construction of the operation '{1}'".Formato(ctx.OperationInfo.OperationType.ToString(), MultiEnumDN.UniqueKey(ctx.OperationInfo.Key)));
            }
        }
    }
}
