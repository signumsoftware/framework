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
using Signum.Utilities.Reflection;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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
                { "operationExecuteMultiple", url =>url.Action((OperationController c)=>c.ExecuteMultiple()) },
                { "operationDelete", url =>url.Action((OperationController c)=>c.Delete()) },
                { "operationDeleteMultiple", url =>url.Action((OperationController c)=>c.DeleteMultiple()) },
                { "operationConstructFrom", url =>url.Action((OperationController c)=>c.ConstructFrom()) },
                { "operationConstructFromMultiple", url =>url.Action((OperationController c)=>c.ConstructFromMany()) },
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
            if (!type.IsEntity() || !OperationLogic.HasConstructOperations(type))
                return true;

            return Manager.HasConstructOperationsAllowedAndVisible(type);
        }

        public static void AddSetting(OperationSettings setting)
        {
            Manager.Settings.GetOrAddDefinition(setting.OverridenType).AddOrThrow(setting.OperationSymbol, setting, "{0} repeated");
        }

        public static void AddSettings(List<OperationSettings> settings)
        {
            foreach (var item in settings)
            {
                AddSetting(item);
            }
        }

        public static void ReplaceSetting(OperationSettings setting)
        {
            Manager.Settings.GetOrAddDefinition(setting.OverridenType)[setting.OperationSymbol] = setting;
            Manager.Settings.ClearCache();
        }

        public static EntityOperationSettings<T> GetEntitySettings<T>(IEntityOperationSymbolContainer<T> operation) where T : class, IEntity
        {
            return Manager.GetSettings<EntityOperationSettings<T>>(typeof(T), operation.Symbol);
        }

        public static ConstructorOperationSettings<T> GetConstructorSettings<T>(ConstructSymbol<T>.Simple operation) where T : class, IEntity
        {
            return Manager.GetSettings<ConstructorOperationSettings<T>>(typeof(T), operation.Symbol);
        }

        public static ContextualOperationSettings<T> GetContextualSettings<T>(IConstructFromManySymbolContainer<T> operation) where T : class, IEntity
        {
            return Manager.GetSettings<ContextualOperationSettings<T>>(typeof(T), operation.Symbol);
        }


        public static ActionResult DefaultExecuteResult(this ControllerBase controller, Entity entity, string prefix = null)
        {
            if (prefix == null)
                prefix = controller.Prefix();

            var request = controller.ControllerContext.HttpContext.Request;


            if (prefix.HasText())
            {
                if (request[ViewDataKeys.AvoidReturnView].HasText())
                    return new ContentResult();

                if (request[ViewDataKeys.ViewMode] == ViewMode.View.ToString())
                    return controller.PopupView(entity, new PopupViewOptions(prefix));
                else
                    return controller.PopupNavigate(entity, new PopupNavigateOptions(prefix));
            }
            else
            {
                if (!entity.IsNew)
                {
                    string newUrl = Navigator.NavigateRoute(entity);
                    if (!request.UrlReferrer.AbsolutePath.Contains(newUrl) && !request[ViewDataKeys.AvoidReturnRedirect].HasText())
                        return controller.RedirectHttpOrAjax(newUrl);
                }

                if (request[ViewDataKeys.AvoidReturnView].HasText())
                    return new ContentResult();

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

        public static ActionResult DefaultConstructResult(this ControllerBase controller, Entity entity, string newPrefix = null, OperationSymbol operation = null)
        {
            var request = controller.ControllerContext.HttpContext.Request;

            if (newPrefix == null)
                newPrefix = request["newPrefix"];

            if (entity == null)
            {
                return controller.JsonNet(new MessageBoxOptions
                {
                    prefix = newPrefix,
                    message = OperationMessage.TheOperation0DidNotReturnAnEntity.NiceToString(operation?.Let(o=>o.NiceToString())),
                });
            }

            if (entity.Modified == ModifiedState.SelfModified)
                controller.ViewData[ViewDataKeys.WriteEntityState] = true;

            if (newPrefix.HasText())
            {
                if (request[ViewDataKeys.AvoidReturnView].HasText())
                    return new ContentResult();

                return controller.PopupNavigate(entity, new PopupNavigateOptions(newPrefix));
            }
            else //NormalWindow
            {
                if (!entity.IsNew && !request[ViewDataKeys.AvoidReturnRedirect].HasText())
                    return controller.RedirectHttpOrAjax(Navigator.NavigateRoute(entity));

                if (request[ViewDataKeys.AvoidReturnView].HasText())
                    return new ContentResult();

                if (request.IsAjaxRequest())
                    return Navigator.NormalControl(controller, entity);
                else
                    return Navigator.NormalPage(controller, entity);
            }
        }

        public static OperationSymbol GetOperationKeyAssert(this ControllerBase controller, Type entityType)
        {
            var operationFullKey = controller.ControllerContext.RequestContext.HttpContext.Request["operationFullKey"];

            var operationSymbol = SymbolLogic<OperationSymbol>.ToSymbol(operationFullKey);

            OperationLogic.AssertOperationAllowed(operationSymbol, entityType, inUserInterface: true);

            return operationSymbol;
        }

        public static OperationSymbol TryGetOperationKeyAsset(this ControllerBase controller, Type entityType)
        {
            var operationFullKey = controller.ControllerContext.RequestContext.HttpContext.Request["operationFullKey"];

            if (operationFullKey == null)
                return null;

            var operationSymbol = SymbolLogic<OperationSymbol>.ToSymbol(operationFullKey);

            OperationLogic.AssertOperationAllowed(operationSymbol, entityType, inUserInterface: true);

            return operationSymbol;
        }

        public static OperationInfo TryGetOperationInfo(this ControllerBase controllerType, Type entityType)
        {
            OperationSymbol operationSymbol = controllerType.TryGetOperationKeyAsset(entityType);

            if (operationSymbol == null)
                return null;

            return OperationLogic.GetOperationInfo(entityType, operationSymbol);
        }

        public static bool IsLite(this Controller controller)
        {
            return controller.Request.RequestContext.HttpContext.Request["isLite"] == "true";
        }
    }

    public class OperationManager
    {
        public Polymorphic<Dictionary<OperationSymbol, OperationSettings>> Settings =
            new Polymorphic<Dictionary<OperationSymbol, OperationSettings>>(PolymorphicMerger.InheritDictionaryInterfaces, typeof(IEntity));

        public OS GetSettings<OS>(Type type, OperationSymbol operation)
            where OS : OperationSettings
        {
            OperationSettings settings = Settings.TryGetValue(type)?.TryGetC(operation);

            if (settings != null)
            {
                var result = settings as OS;

                if (result == null)
                    throw new InvalidOperationException("{0}({1}) should be a {2}".FormatWith(settings.GetType().TypeName(), operation.Key, typeof(OS).TypeName()));

                return result;
            }

            return null;
        }

       
        ConcurrentDictionary<Type, List<OperationInfo>> operationInfoCache = new ConcurrentDictionary<Type, List<OperationInfo>>();
        public IEnumerable<OperationInfo> OperationInfos(Type entityType)
        {
            var result = operationInfoCache.GetOrAdd(entityType, OperationLogic.GetAllOperationInfos);

            return result.Where(oi => OperationLogic.OperationAllowed(oi.OperationSymbol, entityType, true));
        }

        #region Execute ToolBarButton

        static readonly GenericInvoker<Func<Entity, OperationInfo, EntityButtonContext, EntityOperationSettingsBase, IEntityOperationContext>> newEntityOperationContext =
              new GenericInvoker<Func<Entity, OperationInfo, EntityButtonContext, EntityOperationSettingsBase, IEntityOperationContext>>((entity, oi, ctx, settings) =>
                  new EntityOperationContext<Entity>(entity, oi, ctx, (EntityOperationSettings<Entity>)settings));

        public virtual ToolBarButton[] ButtonBar_GetButtonBarElement(EntityButtonContext ctx, ModifiableEntity entity)
        {
            Entity ident = entity as Entity;

            if (ident == null)
                return null;

            Type type = ident.GetType();

            var operations = (from oi in OperationInfos(type)
                              where oi.IsEntityOperation && (oi.AllowsNew.Value || !ident.IsNew)
                              let os = GetSettings<EntityOperationSettingsBase>(type, oi.OperationSymbol)
                              let eoc = newEntityOperationContext.GetInvoker(os?.OverridenType ?? type)(ident, oi, ctx, os)
                              where (os != null && os.HasIsVisible) ? os.OnIsVisible(eoc) : ctx.ShowOperations
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

            foreach (var eoc in operations.Where(c => c.OperationSettings == null || !c.OperationSettings.HideOnCanExecute || c.CanExecute == null))
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

        private EntityOperationGroup GetDefaultGroup(IEntityOperationContext eoc)
        {
            if (eoc.OperationSettings != null && eoc.OperationSettings.Group != null)
                return eoc.OperationSettings.Group == EntityOperationGroup.None ? null : eoc.OperationSettings.Group;

            if (eoc.OperationInfo.OperationType == OperationType.ConstructorFrom)
                return EntityOperationGroup.Create;

            return null;
        }


        public Func<ToolBarButton, ToolBarButton> CustomizeToolBarButton; 
        protected internal virtual ToolBarButton CreateToolBarButton(IEntityOperationContext ctx, EntityOperationGroup group)
        {
            var result = new ToolBarButton(ctx.Context.Prefix, ctx.OperationInfo.OperationSymbol.Key.Replace(".", "_"))
            {
                Style = ctx.OperationSettings?.Style ?? EntityOperationSettingsBase.AutoStyleFunction(ctx.OperationInfo),

                Tooltip = ctx.CanExecute,
                Enabled = ctx.CanExecute == null,
                Order = ctx.OperationSettings != null ? ctx.OperationSettings.Order : 0,

                Text = ctx.OperationSettings?.Text ?? (group == null || group.SimplifyName == null ? ctx.OperationInfo.OperationSymbol.NiceToString() : group.SimplifyName(ctx.OperationInfo.OperationSymbol.NiceToString())),
                OnClick = ((ctx.OperationSettings != null && ctx.OperationSettings.HasClick) ? ctx.OperationSettings.OnClick(ctx) ?? DefaultClick(ctx) : DefaultClick(ctx)),
                HtmlProps = { { "data-operation", ctx.OperationInfo.OperationSymbol.Key } },

                Tag = ctx,
            };

            if (CustomizeToolBarButton != null)
                return CustomizeToolBarButton(result);

            return result;
        }

        protected internal virtual JsFunction DefaultClick(IEntityOperationContext ctx)
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
                    throw new InvalidOperationException("Invalid Operation Type '{0}' in the construction of the operation '{1}'".FormatWith(
                        ctx.OperationInfo.OperationType.ToString(), ctx.OperationInfo.OperationSymbol));
            }
        }
        #endregion

        #region Constructor

        static readonly GenericInvoker<Func<OperationInfo, ClientConstructorContext, ConstructorOperationSettingsBase, IClientConstructorOperationContext>> newClientConstructorOperationContext =
             new GenericInvoker<Func<OperationInfo, ClientConstructorContext, ConstructorOperationSettingsBase, IClientConstructorOperationContext>>((oi, cctx, settings) =>
                new ClientConstructorOperationContext<Entity>(oi, cctx, (ConstructorOperationSettings<Entity>)settings));

    

        protected internal JsFunction ClientConstruct(ClientConstructorContext ctx)
        {
            var dic = (from oi in OperationInfos(ctx.Type)
                       where oi.OperationType == OperationType.Constructor
                       let os = GetSettings<ConstructorOperationSettingsBase>(ctx.Type, oi.OperationSymbol)
                       let coc = newClientConstructorOperationContext.GetInvoker(ctx.Type)(oi, ctx, os)
                       where os != null && os.HasIsVisible ? os.OnIsVisible(coc) : true
                       select coc).ToDictionary(a => a.OperationInfo.OperationSymbol);

            if (dic.Count == 0)
                return null;

            return JsModule.Navigator["chooseConstructor"](ClientConstructorManager.ExtraJsonParams, ctx.Prefix,
                 SelectorMessage.PleaseSelectAConstructor.NiceToString(),
                 dic.Select(kvp => new
                 {
                     value = kvp.Key.Key,
                     toStr = kvp.Value.Settings?.Text ?? kvp.Key.NiceToString(),
                     operationConstructor = kvp.Value.Settings == null || !kvp.Value.Settings.HasClientConstructor ? null : new JRaw(PromiseRequire(kvp.Value.Settings.OnClientConstructor(kvp.Value)))
                 }));
        }

        private string PromiseRequire(JsFunction func)
        {
            return
@"function(extraArgs){ 
    return new Promise(function(resolve){
        require(['{moduleNames}'], function({moduleVars}){
            {moduleVars}.{functionName}({arguments}).then(function(args) { resolve(args); });
        });
    });
}".Replace("{moduleNames}", func.Module.Name)
 .Replace("{moduleVars}",  JsFunction.VarName(func.Module))
 .Replace("{functionName}", func.FunctionName)
 .Replace("{arguments}", func.Arguments.ToString(a => a == ClientConstructorManager.ExtraJsonParams ? "extraArgs" : JsonConvert.SerializeObject(a, func.JsonSerializerSettings), ", "));
        }

        static readonly GenericInvoker<Func<OperationInfo, ConstructorContext, ConstructorOperationSettingsBase, IConstructorOperationContext>> newConstructorOperationContext =
        new GenericInvoker<Func<OperationInfo, ConstructorContext, ConstructorOperationSettingsBase, IConstructorOperationContext>>((oi, ctx, settings) =>
            new ConstructorOperationContext<Entity>(oi, ctx, (ConstructorOperationSettings<Entity>)settings));

        protected internal virtual Entity Construct(ConstructorContext ctx)
        {
            OperationInfo constructor = GetConstructor(ctx);

            var settings = GetSettings<ConstructorOperationSettingsBase>(ctx.Type, constructor.OperationSymbol);

            var result = settings != null && settings.HasConstructor ? settings.OnConstructor(newConstructorOperationContext.GetInvoker(ctx.Type)(constructor, ctx, settings)) :
                OperationLogic.ServiceConstruct(ctx.Type, constructor.OperationSymbol);

            ctx.Controller.ViewData[ViewDataKeys.WriteEntityState] = true;

            return result;
        }

        private OperationInfo GetConstructor(ConstructorContext ctx)
        {
            OperationInfo constructor = OperationInfos(ctx.Type).SingleOrDefaultEx(a => a.OperationType == OperationType.Constructor &&
                (ctx.OperationInfo == null || a.OperationSymbol.Equals(ctx.OperationInfo.OperationSymbol)));

            if (constructor == null)
                throw new InvalidOperationException("No Constructor operation found");

            return constructor;
        }

        internal bool HasConstructOperationsAllowedAndVisible(Type type)
        {
            return OperationInfos(type).Any(oi =>
            {
                if (oi.OperationType != OperationType.Constructor)
                    return false;

                var os = GetSettings<ConstructorOperationSettingsBase>(type, oi.OperationSymbol);

                if (os == null || !os.HasIsVisible)
                    return true;

                var ctx = newClientConstructorOperationContext.GetInvoker(type)(oi, null, os);

                return os.OnIsVisible(ctx);
            });
        }
        #endregion

        static readonly GenericInvoker<Func<SelectedItemsMenuContext, OperationInfo, ContextualOperationSettingsBase, EntityOperationSettingsBase, IContextualOperationContext>> newContextualOperationContext =
         new GenericInvoker<Func<SelectedItemsMenuContext, OperationInfo, ContextualOperationSettingsBase, EntityOperationSettingsBase, IContextualOperationContext>>((ctx, oi, settings, entitySettings) =>
             new ContextualOperationContext<Entity>(ctx, oi, (ContextualOperationSettings<Entity>)settings, (EntityOperationSettings<Entity>)entitySettings));


        public virtual MenuItemBlock ContextualItemsHelper_GetConstructorFromManyMenuItems(SelectedItemsMenuContext ctx)
        {
            if (ctx.Lites.IsNullOrEmpty())
                return null;

            var type = ctx.Lites.Select(a => a.EntityType).Distinct().Only();

            if (type == null)
                return null;

            List<IMenuItem> menuItems =
               (from oi in OperationInfos(type)
                where oi.OperationType == OperationType.ConstructorFromMany
                let os = GetSettings<ContextualOperationSettingsBase>(type, oi.OperationSymbol)
                let coc = newContextualOperationContext.GetInvoker(os?.OverridenType ?? oi.BaseType)(ctx, oi, os, null)
                where os == null || !os.HasIsVisible || os.OnIsVisible(coc)
                 select CreateContextual(coc, _coc => JsModule.Operations["constructFromManyDefault"](_coc.Options(), JsFunction.Event)))
                 .OrderBy(a => a.Order)
                 .Cast<IMenuItem>()
                 .ToList();

            if (menuItems.IsEmpty())
                return null;
            
            return new MenuItemBlock { Header = SearchMessage.Create.NiceToString(), Items = menuItems };
        }


        public virtual MenuItemBlock ContextualItemsHelper_GetEntityOperationMenuItem(SelectedItemsMenuContext ctx)
        {
            if (ctx.Lites.IsNullOrEmpty())
                return null;

            if (ctx.Implementations.IsByAll)
                return null;

            var type = ctx.Lites.Select(a => a.EntityType).Distinct().Only();

            if (type == null)
                return null;

            var context = (from oi in OperationInfos(type)
                           where oi.IsEntityOperation
                           let os = GetSettings<EntityOperationSettingsBase>(type, oi.OperationSymbol)
                           let osc = os == null ? null :
                                     ctx.Lites.Count == 1 ? os.ContextualUntyped: os.ContextualFromManyUntyped
                           let coc = newContextualOperationContext.GetInvoker(os?.OverridenType ?? type)(ctx, oi, osc, os)
                           let defaultBehaviour = oi.Lite == true && (ctx.Lites.Count == 1 || oi.OperationType != OperationType.ConstructorFrom)
                           where os == null ? defaultBehaviour :
                                 !os.ContextualUntyped.HasIsVisible ? defaultBehaviour && !os.HasIsVisible && (!os.HasClick || os.ContextualUntyped.HasClick) :
                                 os.ContextualUntyped.OnIsVisible(coc)
                           select coc).ToList();

            if (context.IsEmpty())
                return null;

            if(ctx.Lites.Count == 1)
            {
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
            }
            else
            {
                var cleanKeys = context.Where(cod => cod.CanExecute == null && cod.OperationInfo.HasStates == true)
                    .Select(kvp => kvp.OperationInfo.OperationSymbol).ToList();

                if (cleanKeys.Any())
                {
                    Dictionary<OperationSymbol, string> canExecutes = OperationLogic.GetContextualCanExecute(ctx.Lites, cleanKeys);
                    foreach (var cod in context)
                    {
                        var ce = canExecutes.TryGetC(cod.OperationInfo.OperationSymbol);
                        if (ce.HasText())
                            cod.CanExecute = ce;
                    }
                }
            }

            List<IMenuItem> menuItems = context
                .Where(coc => !coc.HideOnCanExecute || coc.CanExecute == null)
                .Select(coc => CreateContextual(coc, DefaultEntityClick))
                .OrderBy(a => a.Order).Cast<IMenuItem>().ToList();

            if (menuItems.IsEmpty())
                return null;
            
            return new MenuItemBlock { Header = SearchMessage.Operation.NiceToString(), Items = menuItems };
        }


        protected virtual JsFunction DefaultEntityClick(IContextualOperationContext ctx)
        {
            if (ctx.UntypedEntites.Count() == 1)
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
                        throw new InvalidOperationException("Invalid Operation Type '{0}' in the construction of the operation '{1}'".FormatWith(ctx.OperationInfo.OperationType.ToString(), ctx.OperationInfo.OperationSymbol));
                }
            }
            else
            {
                switch (ctx.OperationInfo.OperationType)
                {
                    case OperationType.Execute:
                        return JsModule.Operations["executeDefaultContextualMultiple"](ctx.Options());
                    case OperationType.Delete:
                        return JsModule.Operations["deleteDefaultContextualMultiple"](ctx.Options());
                    case OperationType.ConstructorFrom:
                        return JsModule.Operations["constructFromDefaultContextualMultiple"](ctx.Options(), JsFunction.Event);
                    default:
                        throw new InvalidOperationException("Invalid Operation Type '{0}' in the construction of the operation '{1}'".FormatWith(ctx.OperationInfo.OperationType.ToString(), ctx.OperationInfo.OperationSymbol));
                }
            }
        }


        public Func<MenuItem, MenuItem> CustomizeMenuItem;
        public virtual MenuItem CreateContextual(IContextualOperationContext ctx, Func<IContextualOperationContext, JsFunction> defaultClick)
        {
            var result = new MenuItem(ctx.Context.Prefix, ctx.OperationInfo.OperationSymbol.Key.Replace(".", "_"))
            {
                Style = ctx.OperationSettings?.Style ?? ctx.EntityOperationSettings?.Style ?? EntityOperationSettingsBase.AutoStyleFunction(ctx.OperationInfo),

                Tooltip = ctx.CanExecute,
                Enabled = ctx.CanExecute == null,

                Order = ctx.OperationSettings != null ? ctx.OperationSettings.Order : 0,

                Text = ctx.OperationSettings?.Text ?? ctx.OperationInfo.OperationSymbol.NiceToString(),
                OnClick = ((ctx.OperationSettings != null && ctx.OperationSettings.HasClick) ? ctx.OperationSettings.OnClick(ctx) ?? defaultClick(ctx) : defaultClick(ctx)),

                Tag = ctx,
            };

            if (CustomizeMenuItem != null)
                return CustomizeMenuItem(result);

            return result;
        }
    }
}
