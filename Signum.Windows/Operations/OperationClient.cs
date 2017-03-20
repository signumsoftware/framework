using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Services;
using Signum.Windows.Operations;
using System.Windows.Media;
using Signum.Utilities;
using System.Reflection;
using Win = System.Windows;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using System.Windows;
using System.Windows.Controls;
using Signum.Entities.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.Windows.Automation;
using Signum.Entities.Basics;
using System.Collections.Concurrent;

namespace Signum.Windows.Operations
{
    public static class OperationClient
    {
        public static OperationManager Manager { get; private set; }

        public static void Start(OperationManager operationManager)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Manager = operationManager;

                Navigator.AddSetting(new EntitySettings<OperationLogEntity>() { View = e => new OperationLog() });

                Navigator.Manager.GetButtonBarElementGlobal += Manager.ButtonBar_GetButtonBarElement;
                Navigator.Manager.IsCreable += Manager_IsCreable;

                SearchControl.GetContextMenuItems += Manager.SearchControl_GetConstructorFromManyMenuItems;
                SearchControl.GetContextMenuItems += Manager.SearchControl_GetEntityOperationMenuItem;

                Server.SetSymbolIds<OperationSymbol>();

                LinksClient.RegisterEntityLinks<Entity>((entity, control) => new[]
                { 
                    entity.GetType() == typeof(OperationLogEntity) ? null : 
                        new QuickLinkExplore(new ExploreOptions(typeof(OperationLogEntity), "Target", entity)
                        {
                            OrderOptions = { new OrderOption("Start") }
                        }){ IsShy = true}
                });
            }
        }

        static bool Manager_IsCreable(Type type)
        {
            if (!type.IsEntity() || !OperationClient.Manager.HasConstructOperations(type))
                return true;

            return Manager.HasConstructOperationsAllowedAndVisible(type);
        }

        public static readonly DependencyProperty ConstructFromOperationKeyProperty =
            DependencyProperty.RegisterAttached("ConstructFromOperationKey", typeof(OperationSymbol), typeof(OperationClient), new UIPropertyMetadata(null));
        public static OperationSymbol GetConstructFromOperationKey(DependencyObject obj)
        {
            return (OperationSymbol)obj.GetValue(ConstructFromOperationKeyProperty);
        }
        public static void SetConstructFromOperationKey(DependencyObject obj, OperationSymbol value)
        {
            obj.SetValue(ConstructFromOperationKeyProperty, value);
        }

        public static ImageSource GetImage(Type type, OperationSymbol operation)
        {
            return Manager.GetImage(operation, Manager.Settings.TryGetValue(type)?.TryGetC(operation));
        }

        public static string GetText(Type type, OperationSymbol operation)
        {
            return Manager.GetText(operation, Manager.Settings.TryGetValue(type)?.TryGetC(operation));
        }

        public static void AddSetting(OperationSettings setting)
        {
            Manager.Settings.GetOrAddDefinition(setting.OverridenType).AddOrThrow(setting.OperationSymbol, setting, "{0} repeated");
            Manager.Settings.ClearCache();
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
    }

    public class OperationManager
    {
        public Polymorphic<Dictionary<OperationSymbol, OperationSettings>> Settings =
            new Polymorphic<Dictionary<OperationSymbol, OperationSettings>>(PolymorphicMerger.InheritDictionaryInterfaces, typeof(IEntity));

        public Func<OperationSymbol, bool> IsSave = e => e.ToString().EndsWith(".Save");

        public List<OperationColor> BackgroundColors = new List<OperationColor>
        {
            new OperationColor(a => a.OperationType == OperationType.Execute && a.Lite == false) { Color = Colors.Blue}, 
            new OperationColor(a => a.OperationType == OperationType.Execute && a.Lite == true) { Color = Colors.Yellow}, 
            new OperationColor(e => e.OperationType == OperationType.Delete ) { Color = Colors.Red }, 
        };

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
            return operationInfoCache.GetOrAdd(entityType, t => Server.Return((IOperationServer o) => o.GetOperationInfos(t)));
        }

        ConcurrentDictionary<Type, bool> hasConstructOperations = new ConcurrentDictionary<Type, bool>();
        public bool HasConstructOperations(Type entityType)
        {
            return hasConstructOperations.GetOrAdd(entityType, t => Server.Return((IOperationServer o) => o.HasConstructOperations(t)));
        }

        static readonly GenericInvoker<Func<Entity, OperationInfo, EntityButtonContext, EntityOperationSettingsBase, IEntityOperationContext>> newEntityOperationContext =
            new GenericInvoker<Func<Entity,OperationInfo,EntityButtonContext,EntityOperationSettingsBase,IEntityOperationContext>>((entity, oi, ctx, settings)=>
                new EntityOperationContext<Entity>(entity, oi, ctx, (EntityOperationSettings<Entity>)settings));

        protected internal virtual List<FrameworkElement> ButtonBar_GetButtonBarElement(object entity, EntityButtonContext ctx)
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
                Dictionary<OperationSymbol, string> canExecutes = Server.Return((IOperationServer os) => os.GetCanExecuteAll(ident));
                foreach (var eoc in operations)
                {
                    var ce = canExecutes.TryGetC(eoc.OperationInfo.OperationSymbol);
                    if (ce != null && ce.HasText())
                        eoc.CanExecute = ce;
                }
            }

            List<FrameworkElement> buttons = new List<FrameworkElement>();
            Dictionary<EntityOperationGroup, ToolBarButton> groups = new Dictionary<EntityOperationGroup,ToolBarButton>();
            Dictionary<EntityOperationGroup, List<FrameworkElement>> groupButtons = new Dictionary<EntityOperationGroup,List<FrameworkElement>>();
          
            foreach (var eoc in operations)
            {
                if (eoc.OperationInfo.OperationType == OperationType.ConstructorFrom &&
                   (eoc.OperationSettings == null || !eoc.OperationSettings.AvoidMoveToSearchControl))
                {
                    if(EntityOperationToolBarButton.MoveToSearchControls(eoc))
                        continue; 
                }

                EntityOperationGroup group = GetDefaultGroup(eoc);

                if(group != null)
                {
                    var list = groupButtons.GetOrCreate(group, () =>
                    {
                        var tbb = EntityOperationToolBarButton.CreateGroupContainer(group);
                        groups.Add(group, tbb);
                        buttons.Add(tbb);
                        return new List<FrameworkElement>();
                    });

                   list.Add(EntityOperationToolBarButton.NewMenuItem(eoc, group));
                }
                else
                {
                    buttons.Add(EntityOperationToolBarButton.NewToolbarButton(eoc));
                }
            }

            foreach (var gr in groups)
            {
                var cm = gr.Value.ContextMenu;
                foreach (var b in groupButtons.GetOrThrow(gr.Key).OrderBy(Common.GetOrder))
                    cm.Items.Add(b);
            }

            return buttons.ToList();
        }

        private EntityOperationGroup GetDefaultGroup(IEntityOperationContext eoc)
        {
            if (eoc.OperationSettings != null && eoc.OperationSettings.Group != null)
                return eoc.OperationSettings.Group == EntityOperationGroup.None ? null : eoc.OperationSettings.Group;

            if (eoc.OperationInfo.OperationType == OperationType.ConstructorFrom)
                return EntityOperationGroup.Create;

            return null;
        }

        protected internal virtual Brush GetBackground(OperationInfo oi, OperationSettings os)
        {
            if (os != null && os.Color != null)
                return new SolidColorBrush(os.Color.Value);

            var bc = BackgroundColors.LastOrDefault(a => a.IsApplicable(oi));
            if (bc != null)
                return new SolidColorBrush(bc.Color);

            return null;
        }

        protected internal virtual ImageSource GetImage(OperationSymbol operation, OperationSettings os)
        {
            if (os != null && os.Icon != null)
                return os.Icon;

            if (IsSave(operation))
                return ImageLoader.GetImageSortName("save.png");

            return null;
        }

        protected internal virtual string GetText(OperationSymbol operation, OperationSettings os)
        {
            if (os != null && os.Text != null)
                return os.Text;

            return operation.NiceToString();
        }

        static readonly GenericInvoker<Func<OperationInfo, ConstructorContext, ConstructorOperationSettingsBase, IConstructorOperationContext>> newConstructorOperationContext = 
             new GenericInvoker<Func<OperationInfo,ConstructorContext, ConstructorOperationSettingsBase, IConstructorOperationContext>>((oi, ctx, settings)=>
                new ConstructorOperationContext<Entity>(oi, ctx, (ConstructorOperationSettings<Entity>)settings));

        protected internal virtual Entity Construct(ConstructorContext ctx)
        {
            var dic = (from oi in OperationInfos(ctx.Type)
                       where oi.OperationType == OperationType.Constructor
                       let os = GetSettings<ConstructorOperationSettingsBase>(ctx.Type, oi.OperationSymbol)
                       let coc = newConstructorOperationContext.GetInvoker(ctx.Type)(oi, ctx, os)
                       where os != null && os.HasIsVisible ? os.OnIsVisible(coc) : true
                       select coc).ToDictionary(a => a.OperationInfo.OperationSymbol);

            if (dic.Count == 0)
                return null;

            OperationSymbol selected = null;
            if (dic.Count == 1)
            {
                selected = dic.Keys.SingleEx();
            }
            else
            {
                if (!SelectorWindow.ShowDialog(dic.Keys.ToArray(), out selected,
                    elementIcon: k => OperationClient.GetImage(ctx.Type, k),
                    elementText: k => OperationClient.GetText(ctx.Type, k),
                    title: SelectorMessage.ConstructorSelector.NiceToString(),
                    message: SelectorMessage.PleaseSelectAConstructor.NiceToString(),
                    owner: Window.GetWindow(ctx.Element)))
                    return null;
            }

            var selCoc = dic[selected];

            if (selCoc.Settings != null && selCoc.Settings.HasConstructor)
                return selCoc.Settings.OnConstructor(selCoc);
            else
                return Server.Return((IOperationServer s) => s.Construct(ctx.Type, selected, ctx.Args));
        }


        static readonly GenericInvoker<Func<SearchControl, OperationInfo, ContextualOperationSettingsBase, IContextualOperationContext>> newContextualOperationContext = 
            new GenericInvoker<Func<SearchControl,OperationInfo,ContextualOperationSettingsBase,IContextualOperationContext>>((sc, oi, settings)=>
                new ContextualOperationContext<Entity>(sc, oi, (ContextualOperationSettings<Entity>)settings));

        protected internal virtual IEnumerable<MenuItem> SearchControl_GetConstructorFromManyMenuItems(SearchControl sc)
        {
            if (sc.SelectedItems.IsNullOrEmpty())
                return null;

            var type = sc.SelectedItems.Select(a => a.EntityType).Distinct().Only();

            if (type == null)
                return null;

            return (from oi in OperationInfos(type)
                    where oi.OperationType == OperationType.ConstructorFromMany
                    let os = GetSettings<ContextualOperationSettingsBase>(type, oi.OperationSymbol)
                    let coc = newContextualOperationContext.GetInvoker(os?.OverridenType ?? oi.BaseType)(sc, oi, os)
                    where os == null || !os.HasIsVisible || os.OnIsVisible(coc)
                    select ConstructFromManyMenuItemConsturctor.Construct(coc))
                    .OrderBy(Common.GetOrder)
                   .ToList();
        }

   
        protected internal virtual IEnumerable<MenuItem> SearchControl_GetEntityOperationMenuItem(SearchControl sc)
        {
            if (sc.SelectedItems.IsNullOrEmpty() || sc.SelectedItems.Count != 1)
                return null;

            if (sc.Implementations.IsByAll)
                return null;

            var type = sc.SelectedItem.EntityType;

            var operations = (from oi in OperationInfos(type)
                              where oi.IsEntityOperation
                              let os = GetSettings<EntityOperationSettingsBase>(type, oi.OperationSymbol)
                              let coc = newContextualOperationContext.GetInvoker(os?.OverridenType ?? sc.SelectedItem.EntityType)(sc, oi, os?.ContextualUntyped)
                              where os == null ? oi.Lite == true :
                                   os.ContextualUntyped.HasIsVisible ? os.ContextualUntyped.OnIsVisible(coc) :
                                   oi.Lite == true && !os.HasIsVisible && (!os.HasClick || os.ContextualUntyped.HasClick)
                              select coc).ToList();

            if (operations.IsEmpty())
                return null;

            if (operations.Any(eomi => eomi.OperationInfo.HasCanExecute == true))
            {
                Dictionary<OperationSymbol, string> canExecutes = Server.Return((IOperationServer os) => os.GetCanExecuteLiteAll(sc.SelectedItem));
                foreach (var coc in operations)
                {
                    var ce = canExecutes.TryGetC(coc.OperationInfo.OperationSymbol);
                    if (ce != null && ce.HasText())
                        coc.CanExecute = ce;
                }
            }

            return operations.Select(coc => EntityOperationMenuItemConsturctor.Construct(coc)).OrderBy(Common.GetOrder);
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

                var ctx = newConstructorOperationContext.GetInvoker(type)(oi, null, os);

                return os.OnIsVisible(ctx);
            });
        }
    }

    public class OperationColor
    {
        public OperationColor(Func<OperationInfo, bool> isApplicable)
        {
            IsApplicable = isApplicable;
        }
        public Func<OperationInfo, bool> IsApplicable;
        public Color Color;
    }
}
