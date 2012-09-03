using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Operations;
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
using Signum.Windows.Extensions.Properties;
using Signum.Utilities.ExpressionTrees;
using System.Windows.Automation;

namespace Signum.Windows.Operations
{
    public static class OperationClient
    {
        public static OperationManager Manager { get; private set; }

        static Dictionary<Type, List<OperationInfo>> QueryOperationInfoCache = new Dictionary<Type, List<OperationInfo>>();

        public static void Start(OperationManager operationManager)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSetting(new EntitySettings<OperationLogDN>(EntityType.ServerOnly) { View = e => new LogOperation() });

                Manager = operationManager;

                ButtonBar.GetButtonBarElement += Manager.ButtonBar_GetButtonBarElement;

                Constructor.ConstructorManager.GeneralConstructor += Manager.ConstructorManager_GeneralConstructor;

                SearchControl.GetCustomMenuItems += (qn, type) =>
                {
                    if (type == null)
                        return null;

                    var infos = QueryOperationInfoCache.GetOrCreate(type, () => Server.Return((IOperationServer o) => o.GetQueryOperationInfos(type)));

                    var list = infos.Where(oi =>
                    {
                        ConstructorFromManySettings set = (ConstructorFromManySettings)Manager.Settings.TryGetC(oi.Key);
                        return set == null || set.IsVisible == null || set.IsVisible(qn, oi);
                    }).ToList();

                    if (list.Count == 0)
                        return null;

                    return new ConstructFromMenuItem { OperationInfos = list };
                };

            }
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

        public static Brush GetBackground(Enum key)
        {
            return Manager.GetBackground(key, Manager.Settings.TryGetC(key));
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

        protected internal virtual List<FrameworkElement> ButtonBar_GetButtonBarElement(object entity, Control entityControl, ViewButtons viewButtons)
        {
            IdentifiableEntity ident = entity as IdentifiableEntity;

            if (ident == null)
                return null;

            var list = Server.Return((IOperationServer s)=>s.GetEntityOperationInfos(ident)); 

            var result = list.Select(oi => 
            {
                Type type  = ident.GetType();
                OperationSettings settings = Settings.TryGetC(oi.Key);
                if(settings != null)
                {
                    if(!settings.GetType().IsInstantiationOf(typeof(EntityOperationSettings<>)))
                        throw new InvalidOperationException("OperationSettings for {0} should be a {1} instead of {2}".Formato(
                            oi.Key, typeof(EntityOperationSettings<>).MakeGenericType(type).TypeName(), settings.GetType()));
 
                    Type supraType = settings.GetType().GetGenericArguments()[0]; 
                    if(!supraType.IsAssignableFrom(type))
                        throw new InvalidOperationException("{0} is not a subclass of {1}".Formato(type, supraType)); 

                    type = supraType; 
                }
                return miGenerateButton.GetInvoker(type)(this, oi, ident, entityControl, viewButtons, settings);
            }).NotNull().ToList();

            return result;
        }

        delegate Win.FrameworkElement GenerateButtonDelegate(OperationManager manager, OperationInfo operationInfo, IdentifiableEntity entity, FrameworkElement entityControl, ViewButtons viewButtons, OperationSettings os); 

        static GenericInvoker<GenerateButtonDelegate> miGenerateButton = new GenericInvoker<GenerateButtonDelegate>(
            (ma, oi, e, ec, vb, os) => ma.GenerateButton<TypeDN>(oi, (TypeDN)e, ec,vb, (EntityOperationSettings<TypeDN>)os));

        protected internal virtual Win.FrameworkElement GenerateButton<T>(OperationInfo operationInfo, T entity, FrameworkElement entityControl, ViewButtons viewButtons, EntityOperationSettings<T> os)
            where T:class, IIdentifiable
        {
            EntityOperationEventArgs<T> args = new EntityOperationEventArgs<T>
            {
                Entity = entity,
                EntityControl = entityControl,
                OperationInfo = operationInfo,
            };

            if (os != null && os.IsVisible != null && !os.IsVisible(args))
                return null;

            if (viewButtons == ViewButtons.Ok && (os == null || !os.VisibleOnOk))
                return null;

            
            if(operationInfo.OperationType == OperationType.ConstructorFrom && (os == null  || !os.AvoidMoveToSearchControl))
            {
                var controls = entityControl.Children<SearchControl>()
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

        private ToolBarButton CreateButton<T>(OperationInfo operationInfo, EntityOperationSettings<T> os, EntityOperationEventArgs<T> args) where T : class, IIdentifiable
        {
            ToolBarButton button = new ToolBarButton
            {
                Content = GetText(operationInfo.Key, os),
                Image = GetImage(operationInfo.Key, os),
                Background = GetBackground(operationInfo.Key, os),
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

        protected internal virtual Brush GetBackground(Enum key, OperationSettings os)
        {
            if (os != null && os.Color != null)
                return new SolidColorBrush(os.Color.Value);

            //if (oi.OperationType == OperationType.Delete) TODO Olmo: Pasar OperationInfo
            //    return new SolidColorBrush(Colors.Red);

            return null;
        }

        protected internal virtual ImageSource GetImage(Enum key, OperationSettings os)
        {
            if (os != null && os.Icon != null)
                return os.Icon;

            return null;
        }

        protected internal virtual string GetText(Enum key, OperationSettings os)
        {
            if (os != null && os.Text != null)
                return os.Text;

            return key.NiceToString();
        }

        private static void DefaultOperationExecute<T>(EntityOperationEventArgs<T> args)
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

        internal object ConstructorManager_GeneralConstructor(Type type, Window win)
        {
            if (!type.IsIIdentifiable())
                return null;

            var list = Server.Return((IOperationServer s)=>s.GetConstructorOperationInfos(type)); 

            var dic = (from oi in list
                       let os = (ConstructorSettings)Settings.TryGetC(oi.Key)
                       where os == null || os.IsVisible == null || os.IsVisible(oi)
                       select new { OperationInfo = oi, OperationSettings = os }).ToDictionary(a => a.OperationInfo.Key);


            if (dic.Count == 0)
                return null;

            Enum selected = null;
            if (list.Count == 1)
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
                return Server.Return((IOperationServer s)=>s.Construct(type, selected)); 
        }
    }
}
