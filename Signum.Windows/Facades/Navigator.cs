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
using System.Reflection;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Utilities.ExpressionTrees;
using Signum.Services;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using Signum.Windows.Operations;
using System.Collections.Concurrent;
using System.Threading;
using Signum.Entities.Basics;

namespace Signum.Windows
{
    public static class Navigator
    {
        public static NavigationManager Manager { get; private set; }

        public static void Start(NavigationManager manager)
        {
            Manager = manager;
        }

        public static void NavigateUntyped(object entity, NavigateOptions options = null)
        {
            Manager.Navigate(entity, options ?? new NavigateOptions());
        }

        public static void Navigate<T>(Lite<T> entity, NavigateOptions options = null)
            where T : class, IEntity
        {
            Manager.Navigate(entity, options ?? new NavigateOptions());
        }

        public static void Navigate<T>(T entity, NavigateOptions options = null)
            where T : IEntity
        {
            Manager.Navigate(entity, options ?? new NavigateOptions());
        }


        public static object ViewUntyped(object entity, ViewOptions options = null) 
        {
            return Manager.View(entity, options ?? new ViewOptions());
        }

        public static Lite<T> View<T>(Lite<T> entity, ViewOptions options = null) 
            where T: class, IEntity
        {
            return (Lite<T>)Manager.View(entity, options ?? new ViewOptions());
        }

        public static T View<T>(T entity, ViewOptions options = null)
            where T : ModifiableEntity
        {
            return (T)Manager.View(entity, options ?? new ViewOptions());
        }

    

        public static DataTemplate FindDataTemplate(FrameworkElement element, Type entityType)
        {
            return Manager.FindDataTemplate(element, entityType);
        }

        public static Type SelectType(Window parent, IEnumerable<Type> implementations, Func<Type, bool> filterType)
        {
            return Manager.SelectTypes(parent, implementations, filterType);
        }

        public static bool IsCreable(Type type, bool isSearch = false)
        {
            return Manager.OnIsCreable(type, isSearch);
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

        public static bool IsNavigable(Type type, bool isSearch = false)
        {
            return Manager.OnIsNavigable(type, null, isSearch);
        }

        public static bool IsNavigable(IEntity entity, bool isSearch = false)
        {
            return Manager.OnIsNavigable(entity.GetType(), entity, isSearch);
        }

        public static void AddSettings(List<EntitySettings> settings)
        {
            Navigator.Manager.EntitySettings.AddRange(settings, s => s.StaticType, s => s, "EntitySettings");
        }

        public static void AddSetting(EntitySettings setting)
        {
            Navigator.Manager.EntitySettings.AddOrThrow(setting.StaticType, setting, "EntitySettings {0} repeated");
        }


        public static void Initialize()
        {
            Navigator.Manager.Initialize();
            Finder.Manager.Initialize();
        }

        public static EntitySettings<T> EntitySettings<T>()
            where T : Entity
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

        public static void OpenIndependentWindow<W>(Func<W> windowConstructor,
            Action<W> afterShown = null, EventHandler closed = null) where W : Window
        {
            Manager.OpenIndependentWindow(windowConstructor, afterShown, closed);
        }
    }

    public class NavigationManager
    {
        public Dictionary<Type, EntitySettings> EntitySettings { get; set; }

        public event Action<NormalWindow, ModifiableEntity> TaskNormalWindow;


        bool multithreaded; 

        public NavigationManager(bool multithreaded)
        {
            this.multithreaded = multithreaded;
            EntitySettings = new Dictionary<Type, EntitySettings>();

            if (!Server.OfflineMode)
            {
                TypeEntity.SetTypeNameCallbacks(
                    t => Server.ServerTypes.GetOrThrow(t).CleanName,
                    Server.TryGetType);

                TypeEntity.SetTypeEntityCallbacks(
                    t => Server.ServerTypes.GetOrThrow(t),
                    tdn => Server.GetType(tdn.CleanName));
            }
        }
        
        public event Action Initializing;
        bool initialized;
        internal void Initialize()
        {
            if (!initialized)
            {
                if (!Server.OfflineMode)
                {
                    //Looking for a better place to do this
                    PropertyRoute.SetFindImplementationsCallback(Navigator.FindImplementations);
                }

                EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler(TextBox_GotFocus));


                TaskNormalWindow += TaskSetIconNormalWindow;

                TaskNormalWindow += TaskSetLabelNormalWindow;

                Initializing?.Invoke();

                initialized = true;
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

        public ImageSource DefaultEntityIcon = ImageLoader.GetImageSortName("entity.png");

      

        void TaskSetIconNormalWindow(NormalWindow nw, ModifiableEntity entity)
        {
            var icon = GetEntityIcon(entity.GetType(), true);
            nw.Icon = icon;
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

      

    
    
        public virtual void Navigate(object entityOrLite, NavigateOptions options)
        {
            if (entityOrLite == null)
                throw new ArgumentNullException("entity");

            Type type = entityOrLite is Lite<Entity> ? ((Lite<Entity>)entityOrLite).EntityType : entityOrLite.GetType();

            OpenIndependentWindow(() =>
            {
                NormalWindow win = CreateNormalWindow();
                win.SetTitleText(NormalWindowMessage.Loading0.NiceToString().FormatWith(type.NiceName()));
                return win;
            },
            afterShown: win =>
            {
                try
                {
                    ModifiableEntity entity = entityOrLite as ModifiableEntity;
                    if (entity == null)
                    {
                        Lite<Entity> lite = (Lite<Entity>)entityOrLite;
                        entity = lite.EntityOrNull ?? Server.RetrieveAndForget(lite);
                    }

                    EntitySettings es = AssertViewableEntitySettings(entity);
                    if (!es.OnIsNavigable(true))
                        throw new Exception("{0} is not navigable".FormatWith(entity));

                    if (entity is EmbeddedEntity)
                        throw new InvalidOperationException("ViewSave is not allowed for EmbeddedEntities");

                    Control ctrl = options.View != null ? options.View() : es.CreateView(entity, null);
                    ctrl = es.OnOverrideView(entity, ctrl);


                    SetNormalWindowEntity(win, (ModifiableEntity)entity, options, es, ctrl);
                }
                catch
                {
                    win.Close();
                    throw;
                }
            }, 
            closed: options.Closed);
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
                entity = Server.Retrieve((Lite<Entity>)entityOrLite);
            }

            EntitySettings es = AssertViewableEntitySettings(entity);
            if (!es.OnIsViewable())
                throw new Exception("{0} is not viewable".FormatWith(entity));

            Control ctrl = options.View ?? es.CreateView(entity, options.PropertyRoute);
            ctrl = es.OnOverrideView(entity, ctrl);

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
                Entity ident = (Entity)result;

                bool saveProtected = ((ViewOptions)options).RequiresSaveOperation ?? EntityKindCache.RequiresSaveOperation(ident.GetType());

                if (GraphExplorer.HasChanges(ident))
                {
                    if (saveProtected)
                        throw new InvalidOperationException("The lite '{0}' of type '{1}' is SaveProtected but has changes. Consider setting SaveProtected = false in ViewOptions".FormatWith(entityOrLite, liteType.TypeName()));

                    return ident.ToLiteFat();
                }

                return ident.ToLite();
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

            win.MainControl = ctrl;
            win.ShowOperations = options.ShowOperations;
            win.ViewMode = options.ViewButtons;

            entity = options.Clone ? (ModifiableEntity)((ICloneable)entity).Clone() : entity;
            win.OnPreEntityLoaded(entity);
            win.DataContext = entity;

            if (options.ReadOnly ?? OnIsReadOnly(entityType, entity))
                Common.SetIsReadOnly(win, true);

            if (options is ViewOptions)
                win.SaveProtected = ((ViewOptions)options).RequiresSaveOperation ??
                    (typeof(Entity).IsAssignableFrom(entityType) && EntityKindCache.RequiresSaveOperation(entityType)); //Matters even on Ok

            TaskNormalWindow?.Invoke(win, entity);

            return win;
        }

        public event Func<Type, bool> IsCreable;

        internal protected virtual bool OnIsCreable(Type type, bool isSearchEntity)
        {
            EntitySettings es = EntitySettings.TryGetC(type);

            if (es != null)
            {
                if (!es.OnIsCreable(isSearchEntity))
                    return false;
            }
            else
            {
                if (type.IsEntity())//HACK
                    return false;
                else
                    return true;
            }

            foreach (var isCreable in IsCreable.GetInvocationListTyped())
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

            foreach (var isReadOnly in IsReadOnly.GetInvocationListTyped())
            {
                if (isReadOnly(type, entity))
                    return true;
            }

            return false;
        }
      
        public event Func<Type, ModifiableEntity, bool> IsViewable;

        protected virtual bool IsViewableBase(Type type, ModifiableEntity entity)
        {
            foreach (var isViewable in IsViewable.GetInvocationListTyped())
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
                throw new InvalidOperationException("No EntitySettings for type {0}".FormatWith(entity.GetType().Name));

            if (!es.HasView())
                throw new InvalidOperationException("No view has been set in the EntitySettings for {0}".FormatWith(entity.GetType().Name));

            if (!IsViewableBase(entity.GetType(), entity))
                throw new InvalidOperationException("Entities of type {0} are not viewable".FormatWith(entity.GetType().Name));

            return es;
        }

        internal protected virtual bool OnIsNavigable(Type type, IEntity entity, bool isSearchEntity)
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


      

        public virtual Type SelectTypes(Window parent, IEnumerable<Type> implementations, Func<Type, bool> filterType)
        {
            if (implementations == null || implementations.Count() == 0)
                throw new ArgumentException("implementations");

            var filtered = implementations.Where(filterType).ToList();

            var only = filtered.Only();
            if (only != null)
                return only;

            if (SelectorWindow.ShowDialog(filtered, out Type sel,
    elementIcon: t => Navigator.Manager.GetEntityIcon(t, true),
    elementText: t => t.NiceName(),
    title: SelectorMessage.TypeSelector.NiceToString(),
    message: SelectorMessage.PleaseSelectAType.NiceToString(),
    owner: parent))
                return sel;
            return null;
        }

        public EntitySettings GetEntitySettings(Type type)
        {
            return EntitySettings.TryGetC(type);
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
                throw new InvalidOperationException("Call {0} firs".FormatWith(name));
        }

        public virtual DataTemplate FindDataTemplate(FrameworkElement element, Type entityType)
        {
            if (entityType.IsLite())
            {
                DataTemplate template = (DataTemplate)element.FindResource(Lite.BaseImplementationType);
                if (template != null)
                    return template;
            }

            if (entityType.IsModifiableEntity() || entityType.IsIEntity())
            {
                DataTemplate template = EntitySettings.TryGetC(entityType)?.DataTemplate;
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

        public event Func<ModifiableEntity, EntityButtonContext, List<FrameworkElement>> GetButtonBarElementGlobal;
        public Dictionary<Type, Func<ModifiableEntity, EntityButtonContext, FrameworkElement>> GetButtonBarElementByType = new Dictionary<Type, Func<ModifiableEntity, EntityButtonContext, FrameworkElement>>();

        public void RegisterGetButtonBarElement<T>(Func<T, EntityButtonContext, FrameworkElement> action) where T: ModifiableEntity
        {
            Func<ModifiableEntity, EntityButtonContext, FrameworkElement> casted = (obj, args) => action((T)obj, args);

            var prev = GetButtonBarElementByType.TryGetC(typeof(T));

            GetButtonBarElementByType[typeof(T)] = prev + casted;
        }

        internal List<FrameworkElement> GetToolbarButtons(ModifiableEntity entity, EntityButtonContext ctx)
        {
            List<FrameworkElement> elements = new List<FrameworkElement>();

            if (GetButtonBarElementGlobal != null)
            {
                elements.AddRange(GetButtonBarElementGlobal.GetInvocationListTyped()
                    .Select(d => d(entity, ctx))
                    .NotNull().SelectMany(d => d).NotNull().ToList());
            }

            var getButtons = GetButtonBarElementByType.TryGetC(entity.GetType());
            if(getButtons != null)
            {
                elements.AddRange(getButtons.GetInvocationListTyped()
                    .Select(d => d(entity, ctx))
                    .NotNull().ToList());
            }

            if (ctx.MainControl is IHaveToolBarElements)
            {
                elements.AddRange(((IHaveToolBarElements)ctx.MainControl).GetToolBarElements(entity, ctx));
            }

            return elements.OrderBy(Common.GetOrder).ToList();
        }


        public event Func<ModifiableEntity, EntityButtonContext, EmbeddedWidget> OnGetEmbeddedWigets;

        public List<EmbeddedWidget> GetEmbeddedWigets(ModifiableEntity entity, EntityButtonContext ctx)
        {
            List<EmbeddedWidget> elements = new List<EmbeddedWidget>();

            if (OnGetEmbeddedWigets != null)
            {
                elements.AddRange(OnGetEmbeddedWigets.GetInvocationListTyped()
                    .Select(d => d(entity, ctx))
                    .NotNull().ToList());
            }

            return elements.ToList();
        }

        protected internal virtual void OpenIndependentWindow<W>(Func<W> windowConstructor, Action<W> afterShown, EventHandler closed) where W : Window
        {
            if (multithreaded)
            {
                Async.ShowInAnotherThread(windowConstructor, afterShown, closed);
            }
            else
            {
                W win = windowConstructor();

                win.Closed += closed;

                win.Show();

                afterShown?.Invoke(win);
            }
        }

        public Control GetCurrentWindow()
        {
            if (multithreaded && Application.Current.Dispatcher != Dispatcher.CurrentDispatcher)
                return Async.GetCurrentWindow();

            return Application.Current.Windows.Cast<Window>().FirstOrDefault(a => a.IsActive);
        }
    }

    public class EmbeddedWidget
    {
        public FrameworkElement Control;
        public EmbeddedWidgetPostion Position;
        public int Order = 0; 
    }

    public enum EmbeddedWidgetPostion
    {
        Top,
        Bottom,
    }
}
