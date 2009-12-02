using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using Signum.Utilities;
using System.Windows.Markup;
using Signum.Entities;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Signum.Windows.Properties;
using System.Reflection;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Utilities.ExpressionTrees;
using Signum.Services;
using System.Windows.Threading;

namespace Signum.Windows
{
    public static class Navigator
    {
        public static NavigationManager Manager { get; private set; }

        public static void Start(NavigationManager navigator)
        {
            navigator.Initialize();

            Manager = navigator;

            //Looking for a better place to do this
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler(TextBox_GotFocus));
        }

        static void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke
            (
                DispatcherPriority.ContextIdle,
                new Action
                (
                    () =>
                    {
                        (sender as TextBox).SelectAll();
                        (sender as TextBox).ReleaseMouseCapture();
                    }
                )
            );
        }

        public static object Find(FindOptions findOptions)
        {
            return Manager.Find(findOptions);
        }

        public static object Find(object queryName)
        {
            return Find(new FindOptions(queryName));
        }

        public static object Find(object queryName, string columnName, object value)
        {
            return Find(new FindOptions(queryName)
            {
                OnLoadMode = OnLoadMode.Search,
                Buttons = SearchButtons.Close,
                FilterOptions = new List<FilterOption>
                {
                    new FilterOption { 
                        ColumnName = columnName, 
                        Operation = FilterOperation.EqualTo, 
                        Value = value }
                },
            });
        }

        public static object View(object entity)
        {
            return Manager.View(entity, new ViewOptions());
        }

        public static object View(object entity, ViewButtons buttons)
        {
            return Manager.View(entity, new ViewOptions { Buttons = buttons });
        }

        public static object View(object entity, ViewOptions viewOptions)
        {
            return Manager.View(entity, viewOptions);
        }

        public static void Admin(AdminOptions adminOptions)
        {
            Manager.Admin(adminOptions);
        }

        internal static EntitySettings GetEntitySettings(Type type)
        {
            return Manager.GetEntitySettings(type);
        }

        internal static QuerySettings GetQuerySettings(object queryName)
        {
            return Manager.GetQuerySettings(queryName);
        }

        public static DataTemplate FindDataTemplate(FrameworkElement element, Type entityType)
        {
            DataTemplate template = GetEntitySettings(entityType).TryCC(ess => ess.DataTemplate);
            if (template != null)
                return template;

            if (typeof(Lite).IsAssignableFrom(entityType))
            {
                template = (DataTemplate)element.FindResource(typeof(Lite));
                if (template != null)
                    return template;
            }

            if (typeof(ModifiableEntity).IsAssignableFrom(entityType) || typeof(IIdentifiable).IsAssignableFrom(entityType))
            {
                template = (DataTemplate)element.FindResource(typeof(ModifiableEntity));
                if (template != null)
                    return template;
            }

            return null;
        }

        public static Type SelectType(Window parent, Type[] implementations)
        {
            return Manager.SelectTypes(parent, implementations);
        }

        internal static bool IsFindable(object queryName)
        {
            return Manager.IsFindable(queryName);
        }

        public static bool IsCreable(Type type, bool admin)
        {
            return Manager.IsCreable(type, admin);
        }

        public static bool IsReadOnly(Type type, bool admin)
        {
            return Manager.IsReadOnly(type, admin);
        }

        public static bool IsViewable(Type type, bool admin)
        {
            return Manager.IsViewable(type, admin);
        }
    }


    public class NavigationManager
    {
        public Dictionary<Type, EntitySettings> Settings { get; set; }
        public Dictionary<object, QuerySettings> QuerySetting { get; set; }
        public Dictionary<Type, TypeDN> ServerTypes { get; private set; }

        public event Func<Type, bool> GlobalIsCreable;
        public event Func<Type, bool> GlobalIsReadOnly;
        public event Func<Type, bool> GlobalIsViewable;
        public event Func<object, bool> GlobalIsFindable;

        public event Action<Window, WindowsType, object> TaskViewWindow;

        public ImageSource DefaultFindIcon = ImageLoader.GetImageSortName("find.png");
        public ImageSource DefaultAdminIcon = ImageLoader.GetImageSortName("admin.png");
        public ImageSource DefaultEntityIcon = ImageLoader.GetImageSortName("entity.png");

        public NavigationManager()
        {
            TaskViewWindow += TaskSetSetIcon;
            TaskViewWindow += TaskSetLabelShortcuts;
        }

        public void TaskSetSetIcon(Window windows, WindowsType winType, object typeOrQueryName)
        {
            switch (winType)
            {
                case WindowsType.View: windows.Icon = GetEntityIcon((Type)typeOrQueryName, true); break;
                case WindowsType.Find: windows.Icon = GetFindIcon(typeOrQueryName, true); break;
                case WindowsType.Admin: windows.Icon = GetEntityIcon((Type)typeOrQueryName, true); break;
                default:
                    break;
            }
        }

        public void TaskSetLabelShortcuts(Window windows, WindowsType winType, object typeOrQueryName)
        {
            if (winType == WindowsType.Find)
                return;

            ShortcutHelper.SetLabelShortcuts(windows);
        }


        public ImageSource GetEntityIcon(Type type, bool useDefault)
        {
            EntitySettings es = Settings.TryGetC(type);
            if (es != null && es.Icon != null)
                return es.Icon;

            return useDefault ? DefaultEntityIcon : null;
        }


        public ImageSource GetFindIcon(object queryName, bool useDefault)
        {
            var qs = QuerySetting.TryGetC(queryName);
            if (qs != null && qs.Icon != null)
                return qs.Icon;

            if (queryName is Type)
            {
                EntitySettings es = Settings.TryGetC((Type)queryName);
                if (es != null && es.Icon != null)
                    return es.Icon;
            }

            return useDefault ? DefaultFindIcon : null;
        }

        public ImageSource GetAdminIcon(Type entityType, bool useDefault)
        {
            EntitySettings es = Settings.TryGetC(entityType);
            if (es != null && es.Icon != null)
                return es.Icon;

            return useDefault ? DefaultAdminIcon : null;
        }

        internal void Initialize()
        {
            if (Settings == null)
                Settings = new Dictionary<Type, EntitySettings>();

            var dic = Server.Return((IQueryServer s) => s.GetQueryNames()).ToDictionary(a => a, a => new QuerySettings()); 
            if (QuerySetting != null)
                dic.SetRange(QuerySetting);
            QuerySetting = dic;

            ServerTypes = Server.Return((IBaseServer s)=>s.ServerTypes()); 

        }

        public virtual string SearchTitle(object queryName)
        {
            return Resources.FinderOf0.Formato(QueryUtils.GetNiceQueryName(queryName));
        }

        public virtual object Find(FindOptions findOptions)
        {
            if (!IsFindable(findOptions.QueryName))
                throw new ApplicationException("The query {0} is not allowed".Formato(findOptions.QueryName));

            SearchWindow sw = new SearchWindow(findOptions.Buttons, findOptions.OnLoadMode)
            {
                QueryName = findOptions.QueryName,
                MultiSelection = findOptions.AllowMultiple,
                FilterOptions = new FreezableCollection<FilterOption>(findOptions.FilterOptions),
                ShowFilters = findOptions.ShowFilters,
                ShowFilterButton = findOptions.ShowFilterButton,
                ShowFooter = findOptions.ShowFooter,
                ShowHeader = findOptions.ShowHeader,
                Title = SearchTitle(findOptions.QueryName)
            };

            EntitySettings es = (findOptions.QueryName as Type).TryCC(t => Settings.TryGetC(t));
            if (TaskViewWindow != null)
                TaskViewWindow(sw, WindowsType.Find, findOptions.QueryName);

            sw.Closed += findOptions.Closed;

            if (findOptions.Modal)
            {
                if (sw.ShowDialog() == true)
                {
                    return sw.Result;
                }
                return null;
            }
            sw.Show();
            return null;
        }


        public virtual object View(object entity, ViewOptions viewOptions)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            Type liteType = null;
            if (entity is Lite)
            {
                liteType = Reflector.ExtractLite(entity.GetType());

                Lite lite = (Lite)entity;

                entity = lite.UntypedEntityOrNull ??
                         (viewOptions.Buttons == ViewButtons.Save ? Server.RetrieveAndForget(lite) :
                                                                   Server.Retrieve(lite));
            }

            if (!IsViewable(entity.GetType(), true))
                throw new ApplicationException("Viewing {0} is not allowed".Formato(entity.GetType()));

            EntitySettings es = Settings.GetOrThrow(entity.GetType(), Resources.NoEntitySettingsForType0);

            bool isReadOnly = viewOptions.ReadOnly ?? IsReadOnly(entity.GetType(), false); 

            Window win = null;
            if (viewOptions.ViewWindow != null)
            {
                win = viewOptions.ViewWindow;
            }
            else if (viewOptions.View != null)
            {
                win = CreateNormalWindow(viewOptions.View, entity, viewOptions.TypeContext, isReadOnly, viewOptions.Buttons);
            }
            else if (es.ViewWindow != null)
            {
                win = es.ViewWindow();
            }
            else if (es.View != null)
            {
                win = CreateNormalWindow(es.View(), entity, viewOptions.TypeContext, isReadOnly, viewOptions.Buttons);
            }
            else
            {
                throw new ApplicationException(Resources.NoNavigationDestinyForType.Formato(entity.GetType()));
            }

            if (isReadOnly)
                Common.SetIsReadOnly(win, true);

            if (TaskViewWindow != null)
                TaskViewWindow(win, WindowsType.View, entity.GetType());

            if (viewOptions.Clone && entity is ICloneable)
                win.DataContext = ((ICloneable)entity).Clone();
            else
                win.DataContext = entity;

            win.Closed += viewOptions.Closed;

            if (viewOptions.Modal)
            {
                bool? ok = win.ShowDialog();
                if (ok != true)
                    return null;

                object result = win.DataContext;
                if (liteType != null)
                {
                    return Lite.Create(liteType, (IdentifiableEntity)result);
                }
                return result;
            }
            else
            {
                win.Show();
            }

            return null;
        }

        private NormalWindow CreateNormalWindow(Control ctrl, object entity, TypeContext typeContext, bool isReadOnly, ViewButtons buttons)
        {

            if (typeContext != null)
                Common.SetTypeContext(ctrl, typeContext);

            return new NormalWindow()
            {
                MainControl = ctrl,
                ButtonBar =
                {
                    ViewButtons = buttons,
                    SaveVisible = buttons == ViewButtons.Save && ShowSave(entity.GetType()) && !isReadOnly,
                    OkVisible = buttons == ViewButtons.Ok
                }
            };
        }

        internal protected virtual bool IsCreable(Type type, bool admin)
        {
            if (GlobalIsCreable != null && !GlobalIsCreable(type))
                return false;

            EntitySettings es = Settings.TryGetC(type);
            if (es == null || es.IsCreable == null)
                return true;

            return es.IsCreable(admin);
        }

        internal protected virtual bool IsReadOnly(Type type, bool admin)
        {
            if (GlobalIsReadOnly != null && GlobalIsReadOnly(type))
                return true;

            EntitySettings es = Settings.TryGetC(type);
            if (es == null || es.IsReadOnly == null)
                return false;

            return es.IsReadOnly(admin);
        }

        internal protected virtual bool IsViewable(Type type, bool admin)
        {
            if (GlobalIsViewable != null && !GlobalIsViewable(type))
                return false;

            EntitySettings es = Settings.TryGetC(type);
            if (es == null)
                return false;

            if (es.IsViewable == null)
                return true;

            return es.IsViewable(admin);
        }

        internal protected virtual bool ShowSave(Type type)
        {
            EntitySettings es = Settings.TryGetC(type);
            if (es != null)
                return es.ShowSave;

            return true;
        }

        internal protected virtual bool IsFindable(object queryName)
        {
            if (GlobalIsFindable != null && !GlobalIsFindable(queryName))
                return false;

            QuerySettings es = QuerySetting.TryGetC(queryName);
            if (es == null)
                return false;

            return true;
        }

        public virtual void Admin(AdminOptions adminOptions)
        {
            Type type = adminOptions.Type;

            EntitySettings es = Settings.GetOrThrow(type, Resources.NoEntitySettingsForType0);

            AdminWindow nw = new AdminWindow(type)
            {
                MainControl = es.View.ThrowIfNullC(Resources.NoAdminControlFor0.Formato(type))(),
            };

            if (TaskViewWindow != null)
                TaskViewWindow(nw, WindowsType.Admin, type);

            nw.Show();
        }

        public virtual Type SelectTypes(Window parent, Type[] implementations)
        {
            if (implementations == null || implementations.Length == 0)
                throw new ArgumentException("implementations");

            if (implementations.Length == 1)
                return implementations[0];

            Type sel;
            if (SelectorWindow.ShowDialog(implementations, t => Navigator.Manager.GetEntityIcon(t, true), 
                t => t.NiceName(), out sel, Properties.Resources.TypeSelector, Properties.Resources.PleaseSelectAType, parent))
                return sel;
            return null;
        }

        public EntitySettings GetEntitySettings(Type type)
        {
            return Settings.TryGetC(type);
        }

        public QuerySettings GetQuerySettings(object queryName)
        {
            return QuerySetting.TryGetC(queryName);
        }

        HashSet<string> loadedModules = new HashSet<string>();
        public bool NotDefined(MethodBase methodBase)
        {
            return loadedModules.Add(methodBase.DeclaringType.TypeName() + "." + methodBase.Name);
        }

        public void AssertDefined(MethodBase methodBase)
        {
            string name = methodBase.DeclaringType.TypeName() + "." + methodBase.Name;

            if (!loadedModules.Contains(name))
                throw new ApplicationException("Call {0} first".Formato(name));
        }
    }
}
