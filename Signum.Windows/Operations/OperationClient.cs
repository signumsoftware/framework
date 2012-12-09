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
using Signum.Windows.Properties;

namespace Signum.Windows.Operations
{
    public static class OperationClient
    {
        public static OperationManager Manager { get; private set; }

        public static void Start(OperationManager operationManager)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EntitySettings<OperationLogDN>(EntityType.System) { View = e => new OperationLog() });

                Manager = operationManager;

                NormalWindow.GetButtonBarElement += Manager.ButtonBar_GetButtonBarElement;

                Constructor.ConstructorManager.GeneralConstructor += Manager.ConstructorManager_GeneralConstructor;

                SearchControl.GetContextMenuItems += Manager.SearchControl_GetConstructorFromManyMenuItems;
                SearchControl.GetContextMenuItems += Manager.SearchControl_GetEntityOperationMenuItem;

                Links.RegisterGlobalLinks((entity, control) => new[]
                { 
                    entity.GetType() == typeof(OperationLogDN) ? null : 
                        new QuickLinkExplore(new ExploreOptions(typeof(OperationLogDN), "Target", entity)){ IsShy = true}
                });
            }
        }

        public static bool SaveProtected(Type type)
        {
            return Manager.SaveProtected(type);
        }

        public static readonly DependencyProperty ConstructFromOperationKeyProperty =
            DependencyProperty.RegisterAttached("ConstructFromOperationKey", typeof(Enum), typeof(OperationClient), new UIPropertyMetadata(null));
        public static Enum GetConstructFromOperationKey(DependencyObject obj)
        {
            return (Enum)obj.GetValue(ConstructFromOperationKeyProperty);
        }

        public static void SetConstructFromOperationKey(DependencyObject obj, Enum value)
        {
            obj.SetValue(ConstructFromOperationKeyProperty, value);
        }

        public static ImageSource GetImage(Enum key)
        {
            return Manager.GetImage(key, Manager.Settings.TryGetC(key));
        }

        public static string GetText(Enum key)
        {
            return Manager.GetText(key, Manager.Settings.TryGetC(key));
        }

        public static void AddSetting(OperationSettings setting)
        {
            Manager.Settings.AddOrThrow(setting.Key, setting, "EntitySettings {0} repeated");
        }

        public static void AddSettings(List<OperationSettings> settings)
        {
            Manager.Settings.AddRange(settings, s => s.Key, s => s, "EntitySettings");
        }
    }

    public class OperationManager
    {
        public Dictionary<Enum, OperationSettings> Settings = new Dictionary<Enum, OperationSettings>();

        public Func<Enum, bool> IsSave = e => e.ToString().StartsWith("Save");

        public List<OperationColor> BackgroundColors = new List<OperationColor>
        {
            new OperationColor(a => a.OperationType == OperationType.Execute && a.Lite == false) { Color = Colors.Blue}, 
            new OperationColor(a => a.OperationType == OperationType.Execute && a.Lite == true) { Color = Colors.Yellow}, 
            new OperationColor(e => e.OperationType == OperationType.Delete ) { Color = Colors.Red }, 
            new OperationColor(e => e.OperationType == OperationType.ConstructorFrom ) { Color = Colors.Green }, 
        };

        protected internal virtual List<FrameworkElement> ButtonBar_GetButtonBarElement(object entity, ButtonBarEventArgs ctx)
        {
            IdentifiableEntity ident = entity as IdentifiableEntity;

            if (ident == null)
                return null;

            if (!OperationInfos(ident.GetType()).Any(a => a.IsEntityOperation))
                return null;

            var list = Server.Return((IOperationServer s)=>s.GetEntityOperationInfos(ident)); 

            var result = list.Select(oi => 
            {
                Type type  = ident.GetType();
                OperationSettings settings = Settings.TryGetC(oi.Key);
                if(settings != null)
                {
                    if (!settings.GetType().IsInstantiationOf(typeof(EntityOperationSettings<>)))
                        throw new InvalidOperationException("{0}([1]) should be a {1} instead".Formato(
                            settings.GetType().TypeName(), OperationDN.UniqueKey(oi.Key), typeof(EntityOperationSettings<>).MakeGenericType(type).TypeName()));
 
                    Type supraType = settings.GetType().GetGenericArguments()[0]; 
                    if(!supraType.IsAssignableFrom(type))
                        throw new InvalidOperationException("{0}({1}) does not match {2}".Formato(settings.GetType().TypeName(), OperationDN.UniqueKey(oi.Key), type.TypeName())); 

                    type = supraType; 
                }
                return miGenerateButton.GetInvoker(type)(this, oi, ident, ctx, settings);
            }).NotNull().ToList();

            return result;
        }

        delegate Win.FrameworkElement GenerateButtonDelegate(OperationManager manager, OperationInfo operationInfo, IdentifiableEntity entity, ButtonBarEventArgs ctx, OperationSettings os); 

        static GenericInvoker<GenerateButtonDelegate> miGenerateButton = new GenericInvoker<GenerateButtonDelegate>(
            (ma, oi, e, args, os) => ma.GenerateButton<TypeDN>(oi, (TypeDN)e, args, (EntityOperationSettings<TypeDN>)os));

        protected internal virtual Win.FrameworkElement GenerateButton<T>(OperationInfo operationInfo, T entity, ButtonBarEventArgs bb, EntityOperationSettings<T> os)
            where T:class, IIdentifiable
        {
            EntityOperationEventArgs<T> args = new EntityOperationEventArgs<T>
            {
                Entity = entity,
                EntityControl = bb.MainControl,
                OperationInfo = operationInfo,
                ViewButtons = bb.ViewButtons,
                SaveProtected = bb.SaveProtected,
            };

            if ((os != null && os.IsVisible != null) ? !os.IsVisible(args) : !bb.SaveProtected)
                return null;

            if(operationInfo.OperationType == OperationType.ConstructorFrom && (os == null  || !os.AvoidMoveToSearchControl))
            {
                var controls = bb.MainControl.Children<SearchControl>()
                    .Where(sc => operationInfo.Key.Equals(OperationClient.GetConstructFromOperationKey(sc)) ||
                    sc.NotSet(OperationClient.ConstructFromOperationKeyProperty) && sc.EntityType == operationInfo.ReturnType).ToList();

                if (controls.Any())
                {
                    foreach (var sc in controls)
                    {
                        if (sc.NotSet(OperationClient.ConstructFromOperationKeyProperty))
                        {
                            OperationClient.SetConstructFromOperationKey(sc, operationInfo.Key);
                        }

                        sc.Create = false;

                        var menu = sc.Child<Menu>(b => b.Name == "menu");

                        var panel = (StackPanel)menu.Parent;

                        var oldButton = panel.Children<ToolBarButton>(tb => tb.Tag is OperationInfo && ((OperationInfo)tb.Tag).Key.Equals(operationInfo.Key)).FirstOrDefault();
                        if (oldButton != null)
                            panel.Children.Remove(oldButton);

                        var index = panel.Children.IndexOf(menu);
                        panel.Children.Insert(index, CreateButton<T>(operationInfo, os, args));
                    }

                    return null;
                }
            }


            ToolBarButton result = CreateButton<T>(operationInfo, os, args);

            return result;
        }

        protected internal virtual ToolBarButton CreateButton<T>(OperationInfo operationInfo, EntityOperationSettings<T> os, EntityOperationEventArgs<T> args) where T : class, IIdentifiable
        {
            ToolBarButton button = new ToolBarButton
            {
                Content = GetText(operationInfo.Key, os),
                Image = GetImage(operationInfo.Key, os),
                Background = GetBackground(operationInfo.Key, os, operationInfo),
                Tag = operationInfo,
            };

            AutomationProperties.SetItemStatus(button, OperationDN.UniqueKey(operationInfo.Key));

            args.SenderButton = button;

            if (operationInfo.CanExecute != null)
            {
                button.ToolTip = operationInfo.CanExecute;
                button.IsEnabled = false;
                ToolTipService.SetShowOnDisabled(button, true);
                AutomationProperties.SetHelpText(button, operationInfo.CanExecute);
            }
            else
            {
                button.Click += (_, __) =>
                {
                    if (args.OperationInfo.CanExecute != null)
                        throw new ApplicationException("Operation {0} is disabled: {1}".Formato(args.OperationInfo.Key, args.OperationInfo.CanExecute));

                    if (os != null && os.Click != null)
                    {
                        IIdentifiable newIdent = os.Click(args);
                        if (newIdent != null)
                            args.EntityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
                    }
                    else
                    {
                        DefaultOperationExecute(args);
                    }
                };
            }
            return button;
        }

        protected internal virtual Brush GetBackground(Enum key, OperationSettings os, OperationInfo oi)
        {
            if (os != null && os.Color != null)
                return new SolidColorBrush(os.Color.Value);

            var bc = BackgroundColors.LastOrDefault(a => a.IsApplicable(oi));
            if (bc != null)
                return new SolidColorBrush(bc.Color);

            return null;
        }

        protected internal virtual ImageSource GetImage(Enum key, OperationSettings os)
        {
            if (os != null && os.Icon != null)
                return os.Icon;

            if (IsSave(key))
                return ImageLoader.GetImageSortName("save.png");

            return null;
        }

        protected internal virtual string GetText(Enum key, OperationSettings os)
        {
            if (os != null && os.Text != null)
                return os.Text;

            return key.NiceToString();
        }

        protected internal virtual void DefaultOperationExecute<T>(EntityOperationEventArgs<T> args)
            where T: class, IIdentifiable
        {
            IdentifiableEntity ident = (IdentifiableEntity)(IIdentifiable)args.Entity;
            if (args.OperationInfo.OperationType == OperationType.Execute)
            {
                if (args.OperationInfo.Lite.Value)
                {
                    if (args.EntityControl.LooseChangesIfAny())
                    {
                        Lite<IdentifiableEntity> lite = ident.ToLite();
                        IIdentifiable newIdent = Server.Return((IOperationServer s) => s.ExecuteOperationLite(lite, args.OperationInfo.Key, null));
                        if (args.OperationInfo.Returns)
                            args.EntityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
                    }
                }
                else
                {
                    IIdentifiable newIdent = Server.Return((IOperationServer s) => s.ExecuteOperation(ident, args.OperationInfo.Key, null));
                    if (args.OperationInfo.Returns)
                        args.EntityControl.RaiseEvent(new ChangeDataContextEventArgs(newIdent));
                }
            }
            else if (args.OperationInfo.OperationType == OperationType.ConstructorFrom)
            {
                if (args.OperationInfo.Lite.Value)
                {
                    if (args.EntityControl.LooseChangesIfAny())
                    {
                        Lite lite = Lite.Create(ident.GetType(), ident);
                        IIdentifiable newIdent = Server.Return((IOperationServer s) => s.ConstructFromLite(lite, args.OperationInfo.Key, null));
                        if (args.OperationInfo.Returns)
                            Navigator.Navigate(newIdent);
                    }
                }
                else
                {
                    IIdentifiable newIdent = Server.Return((IOperationServer s) => s.ConstructFrom(ident, args.OperationInfo.Key, null));
                    if (args.OperationInfo.Returns)
                        Navigator.Navigate(newIdent);
                }
            }
            else if (args.OperationInfo.OperationType == OperationType.Delete)
            {
                if (MessageBox.Show(Window.GetWindow(args.EntityControl), "Are you sure of deleting the entity?", "Delete?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Lite lite = Lite.Create(ident.GetType(), ident);
                    Server.Return((IOperationServer s) => s.Delete(lite, args.OperationInfo.Key, null));
                }
            }
        }

        Dictionary<Type, List<OperationInfo>> operationInfoCache = new Dictionary<Type, List<OperationInfo>>();
        public List<OperationInfo> OperationInfos(Type entityType)
        {
            return operationInfoCache.GetOrCreate(entityType, () => Server.Return((IOperationServer o) => o.GetOperationInfos(entityType)));
        }

        protected internal virtual object ConstructorManager_GeneralConstructor(Type entityType, Window win)
        {
            if (!entityType.IsIIdentifiable())
                return null;

            var dic = (from oi in OperationInfos(entityType)
                       where  oi.OperationType == OperationType.Constructor
                       let os = (ConstructorSettings)Settings.TryGetC(oi.Key)
                       where os == null || os.IsVisible == null || os.IsVisible(oi)
                       select new { OperationInfo = oi, OperationSettings = os }).ToDictionary(a => a.OperationInfo.Key);

            if (dic.Count == 0)
                return null;

            Enum selected = null;
            if (dic.Count == 1)
            {
                selected = dic.Keys.SingleEx();
            }
            else
            {
                if (!SelectorWindow.ShowDialog(dic.Keys.ToArray(), out selected,
                    elementIcon: k => OperationClient.GetImage(k),
                    elementText: k => OperationClient.GetText(k),
                    title: Resources.ConstructorSelector,
                    message: Resources.PleaseSelectAConstructor,
                    owner: win))
                    return null;
            }

            var pair = dic[selected];

            if (pair.OperationSettings != null && pair.OperationSettings.Constructor != null)
                return pair.OperationSettings.Constructor(pair.OperationInfo, win);
            else
                return Server.Return((IOperationServer s)=>s.Construct(entityType, selected)); 
        }

        protected internal virtual IEnumerable<MenuItem> SearchControl_GetConstructorFromManyMenuItems(SearchControl sc)
        {
            if (sc.SelectedItems.IsNullOrEmpty())
                return null;

            var entityType = sc.EntityType;

            if (entityType == null)
                return null;

            return (from oi in OperationInfos(entityType)
                    where oi.OperationType == OperationType.ConstructorFromMany
                    let os = (ContextualOperationSettings)Settings.TryGetC(oi.Key)
                    where os == null || os.OnVisible(sc, oi)
                    select ConstructFromManyMenuItemConsturctor.Construct(sc, oi, os))
                   .ToList();
        }

        class EntityData
        {
            public Enum OperationKey;
            public OperationInfo OperationInfo;
            public EntityOperationSettingsBase Settings;
            public string CanExecute;
        }

        protected internal virtual IEnumerable<MenuItem> SearchControl_GetEntityOperationMenuItem(SearchControl sc)
        {
            if (sc.SelectedItems.IsNullOrEmpty() || sc.SelectedItems.Length != 1)
                return null;

            if (sc.Implementations.IsByAll)
                return null;

            var result = (from t in sc.Implementations.Types
                          from oi in OperationClient.Manager.OperationInfos(sc.SelectedItem.RuntimeType)
                          where oi.IsEntityOperation && oi.Lite == true
                          let os = (EntityOperationSettingsBase)OperationClient.Manager.Settings.TryGetC(oi.Key)
                          where os == null ||
                                (os.Contextual == null ? !os.ClickOverriden : os.Contextual.OnVisible(sc, oi))
                          select new EntityData
                          {
                              OperationKey = oi.Key,
                              OperationInfo = oi,
                              CanExecute = null,
                              Settings = os
                          }).ToList();

            if (result.IsEmpty())
                return null;

            var cleanKeys = result.Where(eomi => eomi.CanExecute == null && eomi.OperationInfo.HasStates)
                .Select(kvp => kvp.OperationKey).ToList();

            if (cleanKeys.Any())
            {
                Dictionary<Enum, string> canExecutes = Server.Return((IOperationServer os) => os.GetContextualCanExecute(sc.SelectedItems, cleanKeys));
                foreach (var pomi in result)
                {
                    var ce = canExecutes.TryGetC(pomi.OperationKey);
                    if (ce.HasText())
                        pomi.CanExecute = ce;
                }
            }

            return result.Select(a => EntityOperationMenuItemConsturctor.Construct(sc, a.OperationInfo, a.CanExecute, a.Settings));
        }

        static HashSet<Type> SaveProtectedCache;
        protected internal virtual bool SaveProtected(Type type)
        {
            if (!type.IsIIdentifiable())
                return false;

            if (SaveProtectedCache == null)
                SaveProtectedCache = Server.Return((IOperationServer o) => o.GetSaveProtectedTypes());

            return SaveProtectedCache.Contains(type);
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
