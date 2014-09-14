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
using Signum.Web.PortableAreas;
using Signum.Web.Controllers;
#endregion

namespace Signum.Web.Operations
{
    public static class OperationClient
    {
        public static OperationManager Manager { get; private set; }

        public static void Start(OperationManager operationManager, bool contextualMenuInSearchWindow)
        {
            Manager = operationManager;

            UrlsRepository.DefaultSFUrls.AddRange(new Dictionary<string, Func<UrlHelper, string>> 
            { 
                { "operationExecute", url =>url.Action((OperationController c)=>c.Execute()) },
                { "operationDelete", url =>url.Action((OperationController c)=>c.Delete()) },
                { "operationConstructFrom", url =>url.Action((OperationController c)=>c.ConstructFrom()) },
                { "operationConstructFromMany", url =>url.Action((OperationController c)=>c.ConstructFromMany()) },
            });

            ButtonBarEntityHelper.RegisterGlobalButtons(Manager.ButtonBar_GetButtonBarElement);
            Navigator.Manager.IsCreable += Manager_IsCreable;

            if (contextualMenuInSearchWindow)
            {
                ContextualItemsHelper.GetContextualItemsForLites += Manager.ContextualItemsHelper_GetConstructorFromManyMenuItems;
                ContextualItemsHelper.GetContextualItemsForLites += Manager.ContextualItemsHelper_GetEntityOperationMenuItem;
            }
        }

        static bool Manager_IsCreable(Type type)
        {
            if (!type.IsIdentifiableEntity() || !OperationLogic.HasConstructOperations(type))
                return true;

            return Manager.HasConstructOperationsAllowedAndVisible(type);
        }


        public static void AddSetting(OperationSettings setting)
        {
            Manager.Settings.AddOrThrow(setting.OperationSymbol, setting, "EntitySettings {0} repeated");
        }

        public static void AddSettings(List<OperationSettings> settings)
        {
            Manager.Settings.AddRange(settings, s => s.OperationSymbol, s => s, "EntitySettings");
        }


        public static ActionResult DefaultExecuteResult(this ControllerBase controller, IdentifiableEntity entity, string prefix = null)
        {
            if (prefix == null)
                prefix = controller.Prefix();

            var request = controller.ControllerContext.HttpContext.Request;

            if (request[ViewDataKeys.AvoidReturnView].HasText())
                return new ContentResult();

            if (prefix.HasText())
            {
                TypeContext tc = TypeContextUtilities.UntypedNew(entity, prefix);
                var popupOptions = request[ViewDataKeys.ViewMode] == ViewMode.View.ToString() ?
                    (PopupOptionsBase)new PopupViewOptions(tc) :
                    (PopupOptionsBase)new PopupNavigateOptions(tc);

                return controller.PopupControl(popupOptions);
            }
            else
            {
                if (!entity.IsNew)
                {
                    string newUrl = Navigator.NavigateRoute(entity);
                    if (!request.UrlReferrer.AbsolutePath.Contains(newUrl) && !request[ViewDataKeys.AvoidReturnRedirect].HasText())
                        return controller.RedirectHttpOrAjax(newUrl);
                }

                if (request.IsAjaxRequest())
                    return Navigator.NormalControl(controller, entity);
                else
                    return Navigator.NormalPage(controller, entity);
            }
        }

        public static ActionResult DefaultDelete(this ControllerBase controller, Type type)
        {
            var request = controller.ControllerContext.HttpContext.Request;

            if (!request[ViewDataKeys.AvoidReturnRedirect].HasText())
                return controller.RedirectHttpOrAjax(Finder.FindRoute(type));

            return new ContentResult();
        }

        public static ActionResult DefaultConstructResult(this ControllerBase controller, IdentifiableEntity entity, string newPrefix = null)
        {
            var request = controller.ControllerContext.HttpContext.Request;

            if (request[ViewDataKeys.AvoidReturnView].HasText())
                return new ContentResult();

            if (newPrefix == null)
                newPrefix = request["newPrefix"];

            if (entity.Modified == ModifiedState.SelfModified)
                controller.ViewData[ViewDataKeys.WriteEntityState] = true;

            if (newPrefix.HasText())
            {
                TypeContext tc = TypeContextUtilities.UntypedNew(entity, newPrefix);
                return controller.PopupControl(new PopupNavigateOptions(tc));
            }
            else //NormalWindow
            {
                if (!entity.IsNew && !request[ViewDataKeys.AvoidReturnRedirect].HasText())
                    return controller.RedirectHttpOrAjax(Navigator.NavigateRoute(entity));

                if (request.IsAjaxRequest())
                    return Navigator.NormalControl(controller, entity);
                else
                    return Navigator.NormalPage(controller, entity);
            }
        }

        public static OperationSymbol GetOperationKeyAssert(this Controller controller)
        {
            var operationFullKey = controller.Request.RequestContext.HttpContext.Request["operationFullKey"];

            var operationSymbol = SymbolLogic<OperationSymbol>.ToSymbol(operationFullKey);

            OperationLogic.AssertOperationAllowed(operationSymbol, inUserInterface: true);

            return operationSymbol;
        }

        public static bool IsLite(this Controller controller)
        {
            return controller.Request.RequestContext.HttpContext.Request["isLite"] == "true";
        }
    }

    public class OperationManager
    {
        public Dictionary<OperationSymbol, OperationSettings> Settings = new Dictionary<OperationSymbol, OperationSettings>();

        public T GetSettings<T>(OperationSymbol operationSymbol)
            where T : OperationSettings
        {
            OperationSettings settings = Settings.TryGetC(operationSymbol);
            if (settings != null)
            {
                var result = settings as T;

                if (result == null)
                    throw new InvalidOperationException("{0}({1}) should be a {2}".Formato(settings.GetType().TypeName(), operationSymbol.Key, typeof(T).TypeName()));

                return result;
            }

            return null;
        }

        ConcurrentDictionary<Type, List<OperationInfo>> operationInfoCache = new ConcurrentDictionary<Type, List<OperationInfo>>();
        public IEnumerable<OperationInfo> OperationInfos(Type entityType)
        {
            var result = operationInfoCache.GetOrAdd(entityType, OperationLogic.GetAllOperationInfos);

            return result.Where(oi => OperationLogic.OperationAllowed(oi.OperationSymbol, true));
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
                              let os = GetSettings<EntityOperationSettings>(oi.OperationSymbol)
                              let eoc = new EntityOperationContext
                              {
                                  Url = ctx.Url,
                                  Entity = (IdentifiableEntity)entity,
                                  OperationInfo = oi,
                                  ViewButtons = ctx.ViewMode,
                                  ShowOperations = ctx.ShowOperations,
                                  PartialViewName = ctx.PartialViewName,
                                  Prefix = ctx.Prefix,
                                  OperationSettings = os,
                              }
                              where (os != null && os.IsVisible != null) ? os.IsVisible(eoc) : ctx.ShowOperations
                              select eoc).ToList();

            if (operations.Any(eoc => eoc.OperationInfo.HasCanExecute == true))
            {
                Dictionary<OperationSymbol, string> canExecutes = OperationLogic.ServiceCanExecute(ident);
                foreach (var eoc in operations)
                {
                    var ce = canExecutes.TryGetC(eoc.OperationInfo.OperationSymbol);
                    if (ce != null && ce.HasText())
                        eoc.CanExecute = ce;
                }
            }

            List<ToolBarButton> buttons = new List<ToolBarButton>();
            Dictionary<EntityOperationGroup, ToolBarDropDown> groups = new Dictionary<EntityOperationGroup, ToolBarDropDown>();

            foreach (var eoc in operations)
            {
                EntityOperationGroup group = GetDefaultGroup(eoc);

                if (group != null)
                {
                    var cm = groups.GetOrCreate(group, () =>
                    {
                        var id = group == EntityOperationGroup.Create ? "tmConstructors" : "";

                        var tbm = new ToolBarDropDown(ctx.Prefix, id)
                        {
                            Title = group.Description(),
                            Text = group.Description(),
                            CssClass = group.CssClass,
                            Items = new List<IMenuItem>(),
                            Order = group.Order,
                        };

                        buttons.Add(tbm);

                        return tbm;
                    });

                    cm.Items.Add(CreateToolBarButton(eoc, group).ToMenuItem());
                }
                else
                {
                    buttons.Add(CreateToolBarButton(eoc, null));
                }
            }

            foreach (var item in buttons.OfType<ToolBarDropDown>())
            {
                item.Items = item.Items.OrderBy(a => ((MenuItem)a).Order).ToList();
            }

            return buttons.OrderBy(a => a.Order).ToArray();
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
            return new ToolBarButton(ctx.Prefix, ctx.OperationInfo.OperationSymbol.Key.Replace(".", "_"))
            {
                Style = EntityOperationSettings.Style(ctx.OperationInfo),

                Tooltip = ctx.CanExecute,
                Enabled = ctx.CanExecute == null,
                Order = ctx.OperationSettings != null ? ctx.OperationSettings.Order : 0,

                Text = ctx.OperationSettings.Try(o => o.Text) ?? (group == null || group.SimplifyName == null ? ctx.OperationInfo.OperationSymbol.NiceToString() : group.SimplifyName(ctx.OperationInfo.OperationSymbol.NiceToString())),
                OnClick = ((ctx.OperationSettings != null && ctx.OperationSettings.OnClick != null) ? ctx.OperationSettings.OnClick(ctx) : DefaultClick(ctx)),
            };
        }

        protected internal virtual JsFunction DefaultClick(EntityOperationContext ctx)
        {
            switch (ctx.OperationInfo.OperationType)
            {
                case OperationType.Execute:
                    return JsModule.Operations["executeDefault"](ctx.Options());
                case OperationType.Delete:
                    return JsModule.Operations["deleteDefault"](ctx.Options());
                case OperationType.ConstructorFrom:
                    return JsModule.Operations["constructFromDefault"](ctx.Options(), JsFunction.Event);
                default:
                    throw new InvalidOperationException("Invalid Operation Type '{0}' in the construction of the operation '{1}'".Formato(
                        ctx.OperationInfo.OperationType.ToString(), ctx.OperationInfo.OperationSymbol));
            }
        }
        #endregion

        #region Constructor
        protected internal virtual ModifiableEntity ConstructSingle(Type type)
        {
            OperationInfo constructor = OperationInfos(type).SingleOrDefaultEx(a => a.OperationType == OperationType.Constructor);

            if (constructor == null)
                return null;

            return OperationLogic.ServiceConstruct(type, constructor.OperationSymbol);
        }

        internal bool HasConstructOperationsAllowedAndVisible(Type type)
        {
            return OperationInfos(type).Any(oi =>
            {
                if (oi.OperationType != OperationType.Constructor)
                    return false;

                var os = GetSettings<ConstructorSettings>(oi.OperationSymbol);
                return os == null || os.IsVisible == null || os.IsVisible(oi);
            });
        }
        #endregion

        public virtual List<IMenuItem> ContextualItemsHelper_GetConstructorFromManyMenuItems(SelectedItemsMenuContext ctx)
        {
            if (ctx.Lites.IsNullOrEmpty())
                return null;

            var types = ctx.Lites.Select(a => a.EntityType).Distinct().ToList();

            List<IMenuItem> menuItems =
                (from t in types
                 from oi in OperationInfos(t)
                 where oi.OperationType == OperationType.ConstructorFromMany
                 group new { t, oi } by oi.OperationSymbol into g
                 let os = GetSettings<ContextualOperationSettings>(g.Key)
                 let coc = new ContextualOperationContext
                 {
                     Url = ctx.Url,
                     Prefix = ctx.Prefix,
                     Entities = ctx.Lites,
                     OperationSettings = os,
                     OperationInfo = g.First().oi,
                     CanExecute = OperationSymbol.NotDefinedForMessage(g.Key, types.Except(g.Select(a => a.t)))
                 }
                 where os == null || os.IsVisible == null || os.IsVisible(coc)
                 select CreateContextual(coc, _coc => JsModule.Operations["constructFromManyDefault"](_coc.Options(), JsFunction.Event)))
                 .OrderBy(a => a.Order)
                 .Cast<IMenuItem>()
                 .ToList();

            if (menuItems.IsEmpty())
                return null;

            menuItems.Insert(0, new MenuItemHeader(SearchMessage.Create.NiceToString()));

            return menuItems;
        }


        public virtual List<IMenuItem> ContextualItemsHelper_GetEntityOperationMenuItem(SelectedItemsMenuContext ctx)
        {
            if (ctx.Lites.IsNullOrEmpty() || ctx.Lites.Count > 1)
                return null;

            List<ContextualOperationContext> context =
                (from oi in OperationInfos(ctx.Lites.Single().EntityType)
                 where oi.IsEntityOperation
                 let os = GetSettings<EntityOperationSettings>(oi.OperationSymbol)
                 let coc = new ContextualOperationContext
                 {
                     Url = ctx.Url,
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
                Dictionary<OperationSymbol, string> canExecutes = OperationLogic.ServiceCanExecute(Database.Retrieve(ctx.Lites.Single()));
                foreach (var coc in context)
                {
                    var ce = canExecutes.TryGetC(coc.OperationInfo.OperationSymbol);
                    if (ce != null)
                        coc.CanExecute = ce;
                }
            }

            List<IMenuItem> menuItems = context.Select(coc => CreateContextual(coc, DefaultEntityClick)).OrderBy(a => a.Order).Cast<IMenuItem>().ToList();

            menuItems.Insert(0, new MenuItemHeader(SearchMessage.Operation.NiceToString()));

            return menuItems;
        }

        protected virtual JsFunction DefaultEntityClick(ContextualOperationContext ctx)
        {
            switch (ctx.OperationInfo.OperationType)
            {
                case OperationType.Execute:
                    return JsModule.Operations["executeDefaultContextual"](ctx.Options());
                case OperationType.Delete:
                    return JsModule.Operations["deleteDefaultContextual"](ctx.Options());
                case OperationType.ConstructorFrom:
                    return JsModule.Operations["constructFromDefaultContextual"](ctx.Options(), JsFunction.Event);
                default:
                    throw new InvalidOperationException("Invalid Operation Type '{0}' in the construction of the operation '{1}'".Formato(ctx.OperationInfo.OperationType.ToString(), ctx.OperationInfo.OperationSymbol));
            }
        }

        public virtual MenuItem CreateContextual(ContextualOperationContext ctx, Func<ContextualOperationContext, JsFunction> defaultClick)
        {
            return new MenuItem(ctx.Prefix, ctx.OperationInfo.OperationSymbol.Key.Replace(".", "_"))
            {
                Style = EntityOperationSettings.Style(ctx.OperationInfo),

                Tooltip = ctx.CanExecute,
                Enabled = ctx.CanExecute == null,

                Order = ctx.OperationSettings != null ? ctx.OperationSettings.Order : 0,

                Text = ctx.OperationSettings.Try(o => o.Text) ?? ctx.OperationInfo.OperationSymbol.NiceToString(),
                OnClick = ((ctx.OperationSettings != null && ctx.OperationSettings.OnClick != null) ? ctx.OperationSettings.OnClick(ctx) : defaultClick(ctx))
            };
        }


    }
}
