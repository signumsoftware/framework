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
                FilterOptions = new List<FilterOptions>
                {
                    new FilterOptions { 
                        ColumnName = columnName, 
                        Operation = FilterOperation.EqualTo, 
                        Value = value }
                },
            });
        }

        public static object View(object entity)
        {
            return View(entity, null);
        }

        public static object View(object entity, TypeContext typeContext)
        {
            Lazy lazy = entity as Lazy;

            ViewButtons vb = lazy != null && (lazy.UntypedEntityOrNull == null || !lazy.UntypedEntityOrNull.IsNew) ? ViewButtons.Save : ViewButtons.Ok;

            return Manager.View(entity, new ViewOptions { Buttons = vb, TypeContext = typeContext });
        }

        public static object View(ViewOptions viewOptions, object entity)
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

            if (typeof(Lazy).IsAssignableFrom(entityType))
            {
                template = (DataTemplate)element.FindResource(typeof(Lazy));
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
            //TaskViewWindow += TaskSetLabelShortcuts;
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

            var dic = Server.Service<IQueryServer>().GetQueryNames().ToDictionary(a => a, a => new QuerySettings());
            if (QuerySetting != null)
                dic.SetRange(QuerySetting);
            QuerySetting = dic;

            ServerTypes = Server.Service<IBaseServer>().ServerTypes();

        }

        public virtual string SearchTitle(object queryName)
        {
            string title = (queryName is Type) ? ((Type)queryName).NiceName() :
                           (queryName is Enum) ? ((Enum)queryName).NiceToString() :
                            queryName.ToString();

            return Resources.FinderOf0.Formato(title);
        }

        public virtual object Find(FindOptions findOptions)
        {
            if (!IsFindable(findOptions.QueryName))
                throw new ApplicationException("The query {0} is not allowed".Formato(findOptions.QueryName));

            SearchWindow sw = new SearchWindow(findOptions.OnLoadMode)
            {
                QueryName = findOptions.QueryName,
                Buttons = findOptions.Buttons,
                MultiSelection = findOptions.AllowMultiple,
                FilterOptions = new FreezableCollection<FilterOptions>(findOptions.FilterOptions),
                Mode = findOptions.FilterMode,
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

            Type lazyType = null;
            if (entity is Lazy)
            {
                lazyType = Reflector.ExtractLazy(entity.GetType());

                entity = viewOptions.Buttons == ViewButtons.Save ? Server.RetrieveAndForget((Lazy)entity) :
                                                                   Server.Retrieve((Lazy)entity);
            }

            if (!IsViewable(entity.GetType(), true))
                throw new ApplicationException("Viewing {0} is not allowed".Formato(entity.GetType()));

            EntitySettings es = Settings.GetOrThrow(entity.GetType(), Resources.NoEntitySettingsForType0);

            Window win = null;
            if (es.ViewWindow != null)
            {
                win = es.ViewWindow();
            }
            else
            {
                if (es.View == null)
                    throw new ApplicationException(Resources.NoNavigationDestinyForType.Formato(entity.GetType()));

                Control ctrl = es.View();
                if (viewOptions.TypeContext != null)
                    Common.SetTypeContext(ctrl, viewOptions.TypeContext);

                NormalWindow nw = new NormalWindow()
                {
                    MainControl = ctrl
                };

                if (ShowOkSave(entity.GetType(), viewOptions.Admin))
                {
                    if (viewOptions.Buttons == ViewButtons.Ok)
                        nw.ButtonBar.OkVisible = true;
                    else
                        nw.ButtonBar.SaveVisible = true;
                }

                win = nw;
            }

            if (IsReadOnly(entity.GetType(), viewOptions.Admin))
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
                if (lazyType != null)
                {
                    return Lazy.Create(lazyType, (IdentifiableEntity)result);
                }
                return result;
            }
            else
            {
                win.Show();
            }

            return null;
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

        internal protected virtual bool ShowOkSave(Type type, bool admin)
        {
            EntitySettings es = Settings.TryGetC(type);
            if (es != null && es.ShowOkSave != null)
                return es.ShowOkSave(admin);

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

            TypeSelectorWindow win = new TypeSelectorWindow { Owner = parent };
            win.Types = implementations;
            if (win.ShowDialog() != true)
                return null;

            return win.SelectedType;
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
