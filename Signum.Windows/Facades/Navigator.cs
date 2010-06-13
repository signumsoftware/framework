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
using System.Collections.ObjectModel;

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
            PropertyRoute.SetFindImplementationsCallback(Server.FindImplementations);
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


        public static void Explore(ExploreOptions options)
        {
            Manager.Explore(options);
        }


        public static Lite<T> FindUnique<T>(string columnName, object value, UniqueType uniqueType)
            where T:class, IIdentifiable
        {
            return (Lite<T>)Manager.FindUnique(new FindUniqueOptions(typeof(T))
            {
                UniqueType = uniqueType,
                FilterOptions = new List<FilterOption>()
                {
                    new FilterOption(columnName, value)
                }
            });
        }

        public static Lite<T> FindUnique<T>(FindUniqueOptions options)
            where T : class, IIdentifiable
        {
            if (options.QueryName == null)
                options.QueryName = typeof(T);

            return (Lite<T>)Manager.FindUnique(options);
        }

        public static Lite FindUnique(FindUniqueOptions options)
        {
            return Manager.FindUnique(options);
        }


        public static Lite<T> Find<T>()
            where T : IdentifiableEntity
        {
            return (Lite<T>)Manager.Find(new FindOptions(typeof(T)));
        }

        public static Lite<T> Find<T>(FindOptions options)
            where T : IdentifiableEntity
        {
            if (options.QueryName == null)
                options.QueryName = typeof(T);

            return (Lite<T>)Manager.Find(options);
        }

        public static Lite Find(FindOptions options)
        {
            return Manager.Find(options);
        }


        public static Lite[] FindMany(FindManyOptions options)
        {
            return Manager.FindMany(options);
        }

        public static Lite<T>[] FindMany<T>()
         where T : IdentifiableEntity
        {
            Lite[] result = Manager.FindMany(new FindManyOptions(typeof(T)));
            if (result == null)
                return null;

            return result.Cast<Lite<T>>().ToArray();
        }

        public static Lite<T>[] FindMany<T>(FindManyOptions options)
            where T : IdentifiableEntity
        {
            if (options.QueryName == null)
                options.QueryName = typeof(T);

            Lite[] result = Manager.FindMany(options);
            if (result == null)
                return null;

            return result.Cast<Lite<T>>().ToArray();
        }

        public static int QueryCount(CountOptions options)
        {
            return Manager.QueryCount(options);
        }

        public static void NavigateUntyped(object entity)
        {
            Manager.Navigate(entity, new NavigateOptions());
        }

        public static void NavigateUntyped(object entity, NavigateOptions options)
        {
            Manager.Navigate(entity, options);
        }

        public static void Navigate<T>(Lite<T> entity)
            where T : class, IIdentifiable
        {
            Manager.Navigate(entity, new NavigateOptions());
        }

        public static void Navigate<T>(Lite<T> entity, NavigateOptions options)
            where T : class, IIdentifiable
        {
            Manager.Navigate(entity, options);
        }

        public static void Navigate<T>(T entity)
            where T : IIdentifiable
        {
            Manager.Navigate(entity, new NavigateOptions());
        }

        public static void Navigate<T>(T entity, NavigateOptions options)
            where T : IIdentifiable
        {
            Manager.Navigate(entity, options);
        }


        public static object ViewUntyped(object entity)
        {
            return Manager.View(entity, new ViewOptions());
        }

        public static object ViewUntyped(object entity, ViewOptions options)    
        {
            return Manager.View(entity, options);
        }

        public static Lite<T> View<T>(Lite<T> entity) 
            where T: class, IIdentifiable
        {
            return (Lite<T>)Manager.View(entity, new ViewOptions());
        }

        public static Lite<T> View<T>(Lite<T> entity, ViewOptions options) 
            where T: class, IIdentifiable
        {
            return (Lite<T>)Manager.View(entity, options);
        }

        public static T View<T>(T entity)
            where T : ModifiableEntity
        {
            return (T)Manager.View(entity, new ViewOptions());
        }

        public static T View<T>(T entity, ViewOptions options)
           where T : ModifiableEntity
        {
            return (T)Manager.View(entity, options);
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

        public event Func<Type, bool> GlobalIsCreable;
        public event Func<Type, bool> GlobalIsReadOnly; //IsNew
        public event Func<Type, bool> GlobalIsViewable;
        public event Func<object, bool> GlobalIsFindable;

        public event Action<AdminWindow, Type> TaskAdminWindow;
        public event Action<NormalWindow, ModifiableEntity> TaskNormalWindow;
        public event Action<SearchWindow, object> TaskSearchWindow;

        public ImageSource DefaultFindIcon = ImageLoader.GetImageSortName("find.png");
        public ImageSource DefaultAdminIcon = ImageLoader.GetImageSortName("admin.png");
        public ImageSource DefaultEntityIcon = ImageLoader.GetImageSortName("entity.png");

        public NavigationManager()
        {
            TaskAdminWindow += TaskSetIconAdminWindow;
            TaskNormalWindow += TaskSetIconNormalWindow;
            TaskSearchWindow += TaskSetIconSearchWindow;

            TaskAdminWindow += TaskSetLabelAdminWindow;
            TaskNormalWindow += TaskSetLabelNormalWindow;    
        }

        void TaskSetIconSearchWindow(SearchWindow sw, object qn)
        {
            sw.Icon = GetFindIcon(qn, true); 
        }

        void TaskSetIconNormalWindow(NormalWindow nw, ModifiableEntity entity)
        {
            nw.Icon = GetEntityIcon(entity.GetType(), true);
        }

        void TaskSetIconAdminWindow(AdminWindow aw, Type type)
        {
            aw.Icon = GetEntityIcon(type, true);
        }


        void TaskSetLabelNormalWindow(NormalWindow nw, ModifiableEntity entity)
        {
            ShortcutHelper.SetLabelShortcuts(nw);
        }

        void TaskSetLabelAdminWindow(AdminWindow aw, Type type)
        {
            ShortcutHelper.SetLabelShortcuts(aw);
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

            var dic = Server.Return((IDynamicQueryServer s) => s.GetQueryNames()).ToDictionary(a => a, a => new QuerySettings()); 
            if (QuerySetting != null)
                dic.SetRange(QuerySetting);
            QuerySetting = dic;
        }

        public virtual string SearchTitle(object queryName)
        {
            return Resources.FinderOf0.Formato(QueryUtils.GetNiceQueryName(queryName));
        }

        public virtual Lite Find(FindOptions options)
        {
            AssertFindable(options.QueryName);

            if (options.ReturnIfOne)
            {
                Lite lite = FindUnique(new FindUniqueOptions(options.QueryName)
                {
                    FilterOptions = options.FilterOptions,
                    UniqueType = UniqueType.SingleOrMany
                });

                if (lite != null)
                {
                    return lite;
                }
            }

            SearchWindow sw = CreateSearchWindow(options);

            sw.MultiSelection = false;

            if (sw.ShowDialog() == true)
            {
                return sw.SelectedItem;
            }
            return null;
        }

        public virtual Lite[] FindMany(FindManyOptions options)
        {
            AssertFindable(options.QueryName);

            SearchWindow sw = CreateSearchWindow(options);
            if (sw.ShowDialog() == true)
            {
                return sw.SelectedItems;
            }
            return null;
        }

        public virtual void Explore(ExploreOptions options)
        {
            AssertFindable(options.QueryName); 

            if (options.NavigateIfOne)
            {
                Lite lite = FindUnique(new FindUniqueOptions(options.QueryName)
                {
                    FilterOptions = options.FilterOptions,
                    UniqueType = UniqueType.SingleOrMany,
                });

                if (lite != null)
                {
                    Navigate(lite, new NavigateOptions());
                    return;
                }
            }

            SearchWindow sw = CreateSearchWindow(options);

            if (options.Closed != null)
                sw.Closed += options.Closed;

            sw.Show();
        }

        public virtual Lite FindUnique(FindUniqueOptions options)
        {
            AssertFindable(options.QueryName);

            SetFilterTokens(options.QueryName, options.FilterOptions);
            SetOrderTokens(options.QueryName, options.OrderOptions);

            var request = new UniqueEntityRequest
            {
                 QueryName = options.QueryName,
                 Filters = options.FilterOptions.Select(f => f.ToFilter()).ToList(),
                 Orders = options.OrderOptions.Select(f => f.ToOrder()).ToList(),
                 UniqueType = options.UniqueType,
            };

            return Server.Return((IDynamicQueryServer s) => s.ExecuteUniqueEntity(request));
        }

        public int QueryCount(CountOptions options)
        {
            AssertFindable(options.QueryName);

            SetFilterTokens(options.QueryName, options.FilterOptions);

            var request = new QueryCountRequest
            {
                QueryName = options.QueryName,
                Filters = options.FilterOptions.Select(f => f.ToFilter()).ToList()
            };

            return Server.Return((IDynamicQueryServer s) => s.ExecuteQueryCount(request));
        }

        public void SetFilterTokens(object queryName, IEnumerable<FilterOption> filters)
        {
            QueryDescription description = GetQueryDescription(queryName);

            foreach (var f in filters)
            {
                f.Token = QueryToken.Parse(description, f.Path); 
                f.RefreshRealValue();
            }
        }

        public void SetOrderTokens(object queryName, IEnumerable<OrderOption> orders)
        {
            QueryDescription description = GetQueryDescription(queryName);

            foreach (var o in orders)
            {
                o.Token = QueryToken.Parse(description, o.Path);
            }
        }

        public void SetUserColumns(object queryName, IList<UserColumnOption> userColumns)
        {
            QueryDescription description = GetQueryDescription(queryName);

            for (int i = 0; i < userColumns.Count; i++)
            {
                UserColumnOption uco = userColumns[i];
                QueryToken token = QueryToken.Parse(description, uco.Path);
                uco.UserColumn = new UserColumn(description.StaticColumns.Count, token)
                {
                    UserColumnIndex = i,
                    DisplayName = uco.DisplayName.DefaultText(token.FullKey())
                };
            }
        }

        internal QueryDescription GetQueryDescription(object queryName)
        {
            QuerySettings settings = GetQuerySettings(queryName);
            return settings.QueryDescription ??
                (settings.QueryDescription = Server.Return((IDynamicQueryServer s) => s.GetQueryDescription(queryName))); 
        }

        protected virtual SearchWindow CreateSearchWindow(FindOptionsBase options)
        {
            SearchWindow sw = new SearchWindow(options.GetSearchMode(), options.SearchOnLoad)
            {
                QueryName = options.QueryName,
                FilterOptions = new FreezableCollection<FilterOption>(options.FilterOptions),
                OrderOptions = new ObservableCollection<OrderOption>(options.OrderOptions),
                UserColumns = new ObservableCollection<UserColumnOption>(options.UserColumnOptions),
                ShowFilters = options.ShowFilters,
                ShowFilterButton = options.ShowFilterButton,
                ShowFooter = options.ShowFooter,
                ShowHeader = options.ShowHeader,
                Title = options.WindowTitle ?? SearchTitle(options.QueryName)
            };

            if (TaskSearchWindow != null)
                TaskSearchWindow(sw, options.QueryName);

            return sw;
        }

        public virtual void Navigate(object entityOrLite, NavigateOptions options)
        {
            if (entityOrLite == null)
                throw new ArgumentNullException("entity");

            ModifiableEntity entity = entityOrLite as ModifiableEntity;
            if (entity == null)
            {
                Lite lite = (Lite)entityOrLite;
                entity = lite.UntypedEntityOrNull ?? Server.RetrieveAndForget(lite);
            }

            AssertViewable(entity.GetType(), true);
            EntitySettings es = Settings[entity.GetType()];

            if (entity is EmbeddedEntity)
                throw new InvalidOperationException("ViewSave is not allowed for EmbeddedEntities");

            Control ctrl = options.View ?? es.CreateView(entity, null);

            Window win = CreateNormalWindow((ModifiableEntity)entity, options, es, ctrl);

            if (options.Closed != null)
                win.Closed += options.Closed;

            win.Show();
        }

        public virtual object View(object entityOrLite, ViewOptions options)
        {
            if (entityOrLite == null)
                throw new ArgumentNullException("entity");

            ModifiableEntity entity = entityOrLite as ModifiableEntity;
            Type liteType = null;
            if (entity == null)
            {
                liteType = Reflector.ExtractLite(entityOrLite.GetType());
                entity = Server.Retrieve((Lite)entityOrLite);
            }

            AssertViewable(entity.GetType(), false);
            EntitySettings es = Settings[entity.GetType()];

            Control ctrl = options.View ?? es.CreateView(entity, options.TypeContext);

            NormalWindow win = CreateNormalWindow((ModifiableEntity)entity, options, es, ctrl);

            if (options.AllowErrors != AllowErrors.Ask)
                win.AllowErrors = options.AllowErrors; 

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

        protected virtual NormalWindow CreateNormalWindow(ModifiableEntity entity, ViewOptionsBase options, EntitySettings es, Control ctrl)
        {
            Type entityType = entity.GetType();

            ViewButtons buttons = options.GetViewButtons();

            bool isReadOnly = options.ReadOnly ?? IsReadOnly(entityType, options.Admin);

            NormalWindow win = new NormalWindow()
            {
                MainControl = ctrl,
                ButtonBar =
                {
                    ViewButtons = buttons,
                    SaveVisible = buttons == ViewButtons.Save && ShowSave(entityType) && !isReadOnly,
                    OkVisible = buttons == ViewButtons.Ok
                },
                DataContext = options.Clone ?((ICloneable)entity).Clone(): entity,
            };

            if (isReadOnly)
                Common.SetIsReadOnly(win, true);

            if (TaskNormalWindow != null)
                TaskNormalWindow(win, entity);

            return win;
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

        internal protected virtual void AssertViewable(Type type, bool admin)
        {
            if (GlobalIsViewable != null && !GlobalIsViewable(type))
                throw new UnauthorizedAccessException(Resources.EntitiesOfType0AreNotAllowed.Formato(type.NiceName())); 

            EntitySettings es = Settings.TryGetC(type);
            if (es == null)
                throw new InvalidOperationException(Resources.NoEntitySettingsForType0.Formato(type.NiceName()));

            if (es.IsViewable != null && !es.IsViewable(admin))
                throw new InvalidOperationException(Resources.EntitiesOfType0AreNotVisibleFromA0Window.Formato(admin ? "admin" : "normal"));
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

        internal protected virtual void AssertFindable(object queryName)
        {      
            QuerySettings es = QuerySetting.TryGetC(queryName);
            if (es == null)
                throw new InvalidOperationException(Properties.Resources.Query0NotRegistered.Formato(queryName));

            if (GlobalIsFindable != null && !GlobalIsFindable(queryName))
                throw new UnauthorizedAccessException(Properties.Resources.Query0NotAllowed.Formato(queryName));
        }

        public virtual void Admin(AdminOptions adminOptions)
        {
            Type type = adminOptions.Type;

            EntitySettings es = Settings.GetOrThrow(type, Resources.NoEntitySettingsForType0);

            AdminWindow nw = CreateAdminWindow(type, es);

            nw.Show();
        }

        private AdminWindow CreateAdminWindow(Type type, EntitySettings es)
        {
            AdminWindow nw = new AdminWindow(type)
            {
                MainControl = es.CreateView(null, null),
            };

            if (TaskAdminWindow != null)
                TaskAdminWindow(nw, type);

            return nw;
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
                throw new InvalidOperationException(Resources.Call0First.Formato(name));
        }

 
    }
}
