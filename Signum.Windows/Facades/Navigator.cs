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

        public static void Start(NavigationManager manager)
        {
            Manager = manager;
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

        public static void QueryCountBatch(CountOptions options, Action<int> onResult, Action @finally)
        {
            Manager.QueryCountBatch(options, onResult, @finally);
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


 
        public static QuerySettings GetQuerySettings(object queryName)
        {
            return Manager.GetQuerySettings(queryName);
        }

        public static DataTemplate FindDataTemplate(FrameworkElement element, Type entityType)
        {
            return Manager.FindDataTemplate(element, entityType);
        }

        public static Type SelectType(Window parent, IEnumerable<Type> implementations)
        {
            return Manager.SelectTypes(parent, implementations);
        }

        public static bool IsFindable(object queryName)
        {
            return Manager.OnIsFindable(queryName);
        }

        public static bool IsCreable(Type type, bool isSearchEntity = false)
        {
            return Manager.OnIsCreable(type, isSearchEntity);
        }

        public static bool IsReadOnly(Type type)
        {
            return Manager.OnIsReadOnly(type, null);
        }

        public static bool IsReadOnly(ModifiableEntity entity)
        {
            return Manager.OnIsReadOnly(entity.GetType(), entity);
        }

        public static bool IsViewable(Type type)
        {
            return Manager.OnIsViewable(type, null);
        }

        public static bool IsViewable(ModifiableEntity entity)
        {
            return Manager.OnIsViewable(entity.GetType(), entity);
        }

        public static bool IsNavigable(Type type, bool isSearchEntity = false)
        {
            return Manager.OnIsNavigable(type, null, isSearchEntity);
        }

        public static bool IsNavigable(IIdentifiable entity, bool isSearchEntity = false)
        {
            return Manager.OnIsNavigable(entity.GetType(), entity, isSearchEntity);
        }

        public static void AddSettings(List<EntitySettings> settings)
        {
            Navigator.Manager.EntitySettings.AddRange(settings, s => s.StaticType, s => s, "EntitySettings");
        }

        public static void AddSetting(EntitySettings setting)
        {
            Navigator.Manager.EntitySettings.AddOrThrow(setting.StaticType, setting, "EntitySettings {0} repeated");
        }

        public static void AddQuerySettings(List<QuerySettings> settings)
        {
            Navigator.Manager.QuerySettings.AddRange(settings, s => s.QueryName, s => s, "QuerySettings");
        }

        public static void AddQuerySetting(QuerySettings setting)
        {
            Navigator.Manager.QuerySettings.AddOrThrow(setting.QueryName, setting, "QuerySettings {0} repeated");
        }

        public static void Initialize()
        {
            Manager.Initialize();
        }

        public static EntitySettings<T> EntitySettings<T>()
            where T : IdentifiableEntity
        {
            return (EntitySettings<T>)EntitySettings(typeof(T));
        }

        public static EmbeddedEntitySettings<T> EmbeddedEntitySettings<T>()
            where T : EmbeddedEntity
        {
            return (EmbeddedEntitySettings<T>)EntitySettings(typeof(T));
        }

        public static EntitySettings EntitySettings(Type type)
        {
            return Manager.EntitySettings.GetOrThrow(type, "No EntitySettings for type {0} found");
        } 


        public static Implementations FindImplementations(PropertyRoute pr)
        {
            return Manager.FindImplementations(pr);
        }
    }

    public class NavigationManager
    {
        public Dictionary<Type, EntitySettings> EntitySettings { get; set; }
        public Dictionary<object, QuerySettings> QuerySettings { get; set; }

        public event Action<NormalWindow, ModifiableEntity> TaskNormalWindow;
        public event Action<SearchWindow, object> TaskSearchWindow;

        public NavigationManager()
        {
            EntitySettings = new Dictionary<Type, EntitySettings>();
            QuerySettings = new Dictionary<object, QuerySettings>();

            Lite.SetTypeNameAndResolveType(Server.GetCleanName, Server.TryGetType);
        }

        public event Action Initializing;
        bool initialized;
        internal void Initialize()
        {
            if (!initialized)
            {
                //Looking for a better place to do this
                PropertyRoute.SetFindImplementationsCallback(Navigator.FindImplementations);
                QueryToken.EntityExtensions = parent => Server.Return((IDynamicQueryServer server) => server.ExternalQueryToken(parent));

                EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler(TextBox_GotFocus));

                CompleteQuerySettings();

                TaskNormalWindow += TaskSetIconNormalWindow;
                TaskSearchWindow += TaskSetIconSearchWindow;

                TaskNormalWindow += TaskSetLabelNormalWindow;    

                if (Initializing != null)
                    Initializing();

                initialized = true;
            }
        }

        void CompleteQuerySettings()
        {
            var dic = Server.Return((IDynamicQueryServer s) => s.GetQueryNames()).ToDictionary(a => a, a => new QuerySettings(a));
            foreach (var kvp in dic)
            {
                if (!QuerySettings.ContainsKey(kvp.Key))
                    QuerySettings.Add(kvp.Key, kvp.Value);
            }
        }

        static void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = (TextBox)sender;
            if(!tb.AcceptsReturn && !tb.AcceptsTab)
                Dispatcher.CurrentDispatcher.BeginInvoke
                (
                    DispatcherPriority.ContextIdle,
                    new Action
                    (
                        () =>
                        {
                            tb.SelectAll();
                            tb.ReleaseMouseCapture();
                        }
                    )
                );
        }

        public ImageSource DefaultFindIcon = ImageLoader.GetImageSortName("find.png");
        public ImageSource DefaultAdminIcon = ImageLoader.GetImageSortName("admin.png");
        public ImageSource DefaultEntityIcon = ImageLoader.GetImageSortName("entity.png");

        void TaskSetIconSearchWindow(SearchWindow sw, object qn)
        {
            sw.Icon = GetFindIcon(qn, true); 
        }

        void TaskSetIconNormalWindow(NormalWindow nw, ModifiableEntity entity)
        {
            nw.Icon = GetEntityIcon(entity.GetType(), true);
        }

        void TaskSetLabelNormalWindow(NormalWindow nw, ModifiableEntity entity)
        {
            ShortcutHelper.SetLabelShortcuts(nw);
        }

        public ImageSource GetEntityIcon(Type type, bool useDefault)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es != null && es.Icon != null)
                return es.Icon;

            return useDefault ? DefaultEntityIcon : null;
        }

        public ImageSource GetFindIcon(object queryName, bool useDefault)
        {
            var qs = QuerySettings.TryGetC(queryName);
            if (qs != null && qs.Icon != null)
                return qs.Icon;

            if (queryName is Type)
            {
                EntitySettings es = EntitySettings.TryGetC((Type)queryName);
                if (es != null && es.Icon != null)
                    return es.Icon;
            }

            return useDefault ? DefaultFindIcon : null;
        }

        public ImageSource GetAdminIcon(Type entityType, bool useDefault)
        {
            EntitySettings es = EntitySettings.TryGetC(entityType);
            if (es != null && es.Icon != null)
                return es.Icon;

            return useDefault ? DefaultAdminIcon : null;
        }

        public virtual string SearchTitle(object queryName)
        {
            return Resources.FinderOf0.Formato(QueryUtils.GetNiceName(queryName));
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
                    UniqueType = UniqueType.Only,
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

        public void QueryCountBatch(CountOptions options, Action<int> onResult, Action @finally)
        {
            AssertFindable(options.QueryName);

            SetFilterTokens(options.QueryName, options.FilterOptions);

            var request = new QueryCountRequest
            {
                QueryName = options.QueryName,
                Filters = options.FilterOptions.Select(f => f.ToFilter()).ToList()
            };

            DynamicQueryBachRequest.Enqueue(request, obj => onResult((int)obj), @finally);
        }

        public void SetFilterTokens(object queryName, IEnumerable<FilterOption> filters)
        {
            QueryDescription description = GetQueryDescription(queryName);

            foreach (var f in filters)
            {
                if (f.Token == null && f.Path.HasText())
                    f.Token = QueryUtils.Parse(f.Path, t => QueryUtils.SubTokens(t, description.Columns));

                f.RefreshRealValue();
            }
        }

        public void SetOrderTokens(object queryName, IEnumerable<OrderOption> orders)
        {
            QueryDescription description = GetQueryDescription(queryName);

            foreach (var o in orders)
            {
                o.Token = QueryUtils.Parse(o.Path, t => QueryUtils.SubTokens(t, description.Columns)); 
            }
        }

        public QueryDescription GetQueryDescription(object queryName)
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
                ColumnOptions = new ObservableCollection<ColumnOption>(options.ColumnOptions),
                ColumnOptionsMode = options.ColumnOptionsMode,
                ElementsPerPage = options.ElementsPerPage ?? FindOptions.DefaultElementsPerPage,
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

            Type type = entityOrLite is Lite? ((Lite)entityOrLite).RuntimeType :entityOrLite.GetType();

            NormalWindow win = CreateNormalWindow();
            win.SetTitleText(Resources.Loading0.Formato(type.NiceName()));
            win.Show();
            
            try
            {
                ModifiableEntity entity = entityOrLite as ModifiableEntity;
                if (entity == null)
                {
                    Lite lite = (Lite)entityOrLite;
                    entity = lite.UntypedEntityOrNull ?? Server.RetrieveAndForget(lite);
                }

                EntitySettings es = AssertViewableEntitySettings(entity);
                if (!es.OnIsNavigable(true))
                    throw new Exception("{0} is not navigable".Formato(entity));

                if (entity is EmbeddedEntity)
                    throw new InvalidOperationException("ViewSave is not allowed for EmbeddedEntities");

                Control ctrl = options.View ?? es.CreateView(entity, null);

                SetNormalWindowEntity(win, (ModifiableEntity)entity, options, es, ctrl);

                if (options.Closed != null)
                    win.Closed += options.Closed;
            }
            catch
            {
                win.Close();
                throw;
            }
        }

        public virtual object View(object entityOrLite, ViewOptions options)
        {
            if (entityOrLite == null)
                throw new ArgumentNullException("entity");

            ModifiableEntity entity = entityOrLite as ModifiableEntity;
            Type liteType = null;
            if (entity == null)
            {
                liteType = Lite.Extract(entityOrLite.GetType());
                entity = Server.Retrieve((Lite)entityOrLite);
            }

            EntitySettings es = AssertViewableEntitySettings(entity);
            if (!es.OnIsViewable())
                throw new Exception("{0} is not viewable".Formato(entity));
            
            Control ctrl = options.View ?? es.CreateView(entity, options.TypeContext);

            NormalWindow win = CreateNormalWindow();
                
            SetNormalWindowEntity(win, (ModifiableEntity)entity, options, es, ctrl);

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

        protected virtual NormalWindow CreateNormalWindow()
        {
            return new NormalWindow();
        }

        protected virtual NormalWindow SetNormalWindowEntity(NormalWindow win, ModifiableEntity entity, ViewOptionsBase options, EntitySettings es, Control ctrl)
        {
            Type entityType = entity.GetType();

            ViewButtons buttons = options.ViewButtons;

            bool isReadOnly = options.ReadOnly ?? OnIsReadOnly(entity.GetType(), entity);

            win.MainControl = ctrl;
            win.ButtonBar.ViewButtons = buttons;
            win.ButtonBar.SaveVisible = buttons == ViewButtons.Save  && !isReadOnly && !OnSaveProtected(entity.GetType());
            win.ButtonBar.OkVisible = buttons == ViewButtons.Ok;
            win.DataContext = options.Clone ? ((ICloneable)entity).Clone() : entity;

            if (isReadOnly)
                Common.SetIsReadOnly(win, true);

            if (TaskNormalWindow != null)
                TaskNormalWindow(win, entity);

            return win;
        }

        public event Func<Type, bool> IsCreable;

        internal protected virtual bool OnIsCreable(Type type, bool isSearchEntity)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es == null)
                return true;

            if (!es.OnIsCreable(isSearchEntity))
                return false;


            if (IsCreable != null)
                foreach (Func<Type, bool> isCreable in IsCreable.GetInvocationList())
                {
                    if (!isCreable(type))
                        return false;
                }

            return true;
        }

        public event Func<Type, ModifiableEntity, bool> IsReadOnly;

        internal protected virtual bool OnIsReadOnly(Type type, ModifiableEntity entity)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es != null)
            {
                if (es.OnIsReadonly())
                    return true;
            }

            if (IsReadOnly != null)
                foreach (Func<Type, ModifiableEntity, bool> isReadOnly in IsReadOnly.GetInvocationList())
                {
                    if (isReadOnly(type, entity))
                        return true;
                }

            return false;
        }
      
        public event Func<Type, ModifiableEntity, bool> IsViewable;

        protected virtual bool IsViewableBase(Type type, ModifiableEntity entity)
        {
            if (IsViewable != null)
                foreach (Func<Type, ModifiableEntity, bool> isViewable in IsViewable.GetInvocationList())
                {
                    if (!isViewable(type, entity))
                        return false;
                }

            return true;
        }

        internal protected virtual EntitySettings AssertViewableEntitySettings(ModifiableEntity entity)
        {
            EntitySettings es = EntitySettings.TryGetC(entity.GetType());
            if (es == null)
                throw new InvalidOperationException("No EntitySettings for type {0}".Formato(entity.GetType().Name));

            if (!es.HasView())
                throw new InvalidOperationException("No view has been set in the EntitySettings for {0}".Formato(entity.GetType().Name));

            if (!IsViewableBase(entity.GetType(), entity))
                throw new InvalidOperationException("Entities of type {0} are not viewable".Formato(entity.GetType().Name));

            return es;
        }

        internal protected virtual bool OnIsNavigable(Type type, IIdentifiable entity, bool isSearchEntity)
        {
            EntitySettings es = EntitySettings.TryGetC(type);

            return
                es != null &&
                es.HasView() &&
                IsViewableBase(type, (ModifiableEntity)entity) &&
                es.OnIsNavigable(isSearchEntity);
        }

        internal protected virtual bool OnIsViewable(Type type, ModifiableEntity entity)
        {
            EntitySettings es = EntitySettings.TryGetC(type);

            return
                es != null &&
                es.HasView() &&
                IsViewableBase(type, entity) &&
                es.OnIsViewable();
        }


        public event Func<Type, bool> SaveProtected;

        public bool OnSaveProtected(Type type)
        {
            if (SaveProtected != null)
                foreach (Func<Type, bool> sp in SaveProtected.GetInvocationList())
                {
                    if (sp(type))
                        return true;
                }

            return false;
        }

        public event Func<object, bool> IsFindable;

        internal protected virtual bool OnIsFindable(object queryName)
        {
            QuerySettings es = QuerySettings.TryGetC(queryName);
            if (es == null || !es.IsFindable)
                return false;

            if (IsFindable != null)
                foreach (Func<object, bool> isFindable in IsFindable.GetInvocationList())
                {
                    if (!isFindable(queryName))
                        return false;
                }

            return true;
        }

        internal protected virtual void AssertFindable(object queryName)
        {      
            QuerySettings es = QuerySettings.TryGetC(queryName);
            if (es == null)
                throw new InvalidOperationException(Properties.Resources.Query0NotRegistered.Formato(queryName));

            if (!OnIsFindable(queryName))
                throw new UnauthorizedAccessException(Properties.Resources.Query0NotAllowed.Formato(queryName));
        }

        public virtual Type SelectTypes(Window parent, IEnumerable<Type> implementations)
        {
            if (implementations == null || implementations.Count() == 0)
                throw new ArgumentException("implementations");

            var only = implementations.Only();
            if (only != null)
                return only;

            Type sel;
            if (SelectorWindow.ShowDialog(implementations, out sel,
                elementIcon: t => Navigator.Manager.GetEntityIcon(t, true),
                elementText: t => t.NiceName(),
                title: Properties.Resources.TypeSelector,
                message: Properties.Resources.PleaseSelectAType,
                owner: parent))
                return sel;
            return null;
        }

        public EntitySettings GetEntitySettings(Type type)
        {
            return EntitySettings.TryGetC(type);
        }

        public QuerySettings GetQuerySettings(object queryName)
        {
            return QuerySettings.TryGetC(queryName);
        }

        HashSet<string> loadedModules = new HashSet<string>();
        public bool NotDefined(MethodBase methodBase)
        {
            return loadedModules.Add(methodBase.DeclaringType.FullName + "." + methodBase.Name);
        }

        public void AssertDefined(MethodBase methodBase)
        {
            string name = methodBase.DeclaringType.FullName + "." + methodBase.Name;

            if (!loadedModules.Contains(name))
                throw new InvalidOperationException(Resources.Call0First.Formato(name));
        }

        public virtual DataTemplate FindDataTemplate(FrameworkElement element, Type entityType)
        {
            if (entityType.IsLite())
            {
                DataTemplate template = (DataTemplate)element.FindResource(typeof(Lite));
                if (template != null)
                    return template;
            }

            if (entityType.IsModifiableEntity() || entityType.IsIIdentifiable())
            {
                DataTemplate template = EntitySettings.TryGetC(entityType).TryCC(ess => ess.DataTemplate);
                if (template != null)
                    return template;

                template = (DataTemplate)element.FindResource(typeof(ModifiableEntity));
                if (template != null)
                    return template;
            }

            return null;
        }

        protected internal virtual Implementations FindImplementations(PropertyRoute pr)
        {
            if (typeof(ModelEntity).IsAssignableFrom(pr.RootType))
            {
                EntitySettings es = EntitySettings.TryGetC(pr.RootType);

                if (es != null) //Not mandatory, on windows it's usual not to register model entities. 
                    return es.FindImplementations(pr);

                return ModelEntity.GetImplementations(pr);
            }
            else
            {
                return Server.FindImplementations(pr);
            }
        }
    }
}
