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
                Manager = operationManager;

                Navigator.AddSetting(new EntitySettings<OperationLogDN>() { View = e => new OperationLog() });

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

        Dictionary<Type, List<OperationInfo>> operationInfoCache = new Dictionary<Type, List<OperationInfo>>();
        public List<OperationInfo> OperationInfos(Type entityType)
        {
            return operationInfoCache.GetOrCreate(entityType, () => Server.Return((IOperationServer o) => o.GetOperationInfos(entityType)));
        }

        protected internal virtual List<FrameworkElement> ButtonBar_GetButtonBarElement(object entity, ButtonBarEventArgs ctx)
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
                                  EntityControl = ctx.MainControl,
                                  OperationInfo = oi,
                                  ViewButtons = ctx.ViewButtons,
                                  SaveProtected = ctx.SaveProtected,
                                  OperationSettings = os,
                              }
                              where (os != null && os.IsVisible != null) ? os.IsVisible(eoc) : ctx.SaveProtected
                              select eoc).ToList();

            if (operations.Any(eoc => eoc.OperationInfo.HasCanExecute == true))
            {
                Dictionary<Enum, string> canExecutes = Server.Return((IOperationServer os) => os.GetCanExecute(ident));
                foreach (var eoc in operations)
                {
                    var ce = canExecutes.TryGetC(eoc.OperationInfo.Key);
                    if (ce != null && ce.HasText())
                        eoc.CanExecute = ce;
                }
            }

            return operations.Select(EntityOperationToolBarButton.CreateButton).ToList();
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



        protected internal virtual object ConstructorManager_GeneralConstructor(Type entityType, Window win)
        {
            if (!entityType.IsIIdentifiable())
                return null;

            var dic = (from oi in OperationInfos(entityType)
                       where oi.OperationType == OperationType.Constructor
                       let os = GetSettings<ConstructorSettings>(oi.Key)
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
                return Server.Return((IOperationServer s) => s.Construct(entityType, selected));
        }


        protected internal virtual IEnumerable<MenuItem> SearchControl_GetConstructorFromManyMenuItems(SearchControl sc)
        {
            if (sc.SelectedItems.IsNullOrEmpty())
                return null;

            var types = sc.SelectedItems.Select(a => a.EntityType).Distinct().ToList();

            return (from t in types
                    from oi in OperationInfos(t)
                    where oi.OperationType == OperationType.ConstructorFromMany
                    group new { t, oi } by oi.Key into g
                    let os = GetSettings<ContextualOperationSettings>(g.Key)
                    let coc = new ContextualOperationContext
                    {
                        Entities = sc.SelectedItems,
                        SearchControl = sc,
                        OperationSettings = os,
                        OperationInfo = g.First().oi,
                        CanExecute = OperationDN.NotDefinedFor(g.Key, types.Except(g.Select(a => a.t)))
                    }
                    where os == null || os.IsVisible == null || os.IsVisible(coc)
                    select ConstructFromManyMenuItemConsturctor.Construct(coc))
                   .ToList();
        }

        protected internal virtual IEnumerable<MenuItem> SearchControl_GetEntityOperationMenuItem(SearchControl sc)
        {
            if (sc.SelectedItems.IsNullOrEmpty() || sc.SelectedItems.Length != 1)
                return null;

            if (sc.Implementations.IsByAll)
                return null;

            var operations = (from oi in OperationInfos(sc.SelectedItem.EntityType)
                              where oi.IsEntityOperation
                              let os = GetSettings<EntityOperationSettings>(oi.Key)
                              let coc = new ContextualOperationContext
                              {
                                  Entities = sc.SelectedItems,
                                  SearchControl = sc,
                                  OperationSettings = os == null ? null : os.Contextual,
                                  OperationInfo = oi,
                              }
                              where os == null ? oi.Lite == true :
                                    os.Contextual == null ? (oi.Lite == true && os.Click == null) :
                                    (os.Contextual.IsVisible == null || os.Contextual.IsVisible(coc))
                              select coc).ToList();

            if (operations.IsEmpty())
                return null;

            if (operations.Any(eomi => eomi.OperationInfo.HasCanExecute == true))
            {
                Dictionary<Enum, string> canExecutes = Server.Return((IOperationServer os) => os.GetCanExecuteLite(sc.SelectedItem));
                foreach (var coc in operations)
                {
                    var ce = canExecutes.TryGetC(coc.OperationInfo.Key);
                    if (ce != null && ce.HasText())
                        coc.CanExecute = ce;
                }
            }

            return operations.Select(coc => EntityOperationMenuItemConsturctor.Construct(coc));
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
