using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Utilities;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using System.Reflection;
using Signum.Utilities.Reflection;
using System.Collections.Specialized;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;
using System.Configuration;
using Signum.Utilities.ExpressionTrees;
using Signum.Web.Controllers;
using System.Web.Hosting;
using System.Web.Compilation;
using Signum.Web.PortableAreas;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Engine.Operations;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.UI;
using Signum.Services;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.SqlServer.Types;
using Newtonsoft.Json;
using System.Globalization;

namespace Signum.Web
{
    public static class Navigator
    {
        public static NavigationManager Manager;

        public static void Start(NavigationManager manager)
        {
            Manager = manager;
        }

        public const string ViewRouteName = "sfView";
        public const string CreateRouteName = "sfCreate";

        public static Func<UrlHelper, Type, PrimaryKey?, string> NavigateRouteFunc;
        public static string NavigateRoute(Type type, PrimaryKey? id)
        {
            var entitySettings = EntitySettings(type);
            if (entitySettings.ViewRoute != null)
                return entitySettings.ViewRoute(new UrlHelper(HttpContext.Current.Request.RequestContext), type, id);

            if (NavigateRouteFunc != null)
                return NavigateRouteFunc(new UrlHelper(HttpContext.Current.Request.RequestContext), type, id);


            var result = new UrlHelper(HttpContext.Current.Request.RequestContext).RouteUrl(id == null ? CreateRouteName : ViewRouteName, new
            {
                webTypeName = EntitySettings(type).WebTypeName,
                id = id?.ToString()
            });

            return result;
        }

        public static string NavigateRoute(IEntity ie)
        {
            return NavigateRoute(ie.GetType(), ie.Id);
        }

        public static string NavigateRoute(Lite<IEntity> lite)
        {
            return NavigateRoute(lite.EntityType, lite.Id);
        }

        public static JsonNetResult JsonNet(this ControllerBase controller, object data, JsonSerializerSettings settings = null)
        {
            var result = new JsonNetResult(data);

            if (settings != null)
                result.SerializerSettings = settings;

            return result;
        }

        public static ViewResult NormalPage(this ControllerBase controller, IRootEntity entity, NavigateOptions options = null)
        {
            return Manager.NormalPage(controller, entity, options ?? new NavigateOptions());
        }

        public static PartialViewResult NormalControl(this ControllerBase controller, IRootEntity entity, NavigateOptions options = null)
        {
            return Manager.NormalControl(controller, entity, options ?? new NavigateOptions());
        }

        public static string GetOrCreateTabID(ControllerBase c)
        {
            return Manager.GetOrCreateTabID(c);
        }

        public static string TabID(this ControllerBase controller)
        {
            NameValueCollection form = controller.ControllerContext.HttpContext.Request.Form;

            string tabID;
            if (!form.AllKeys.Contains(ViewDataKeys.TabId) || !(tabID = (string)form[ViewDataKeys.TabId]).HasText())
                throw new InvalidOperationException("The Request doesn't have the necessary tab identifier");           
            
            return tabID;
        }

        public static PartialViewResult PopupView(this ControllerBase controller, ModifiableEntity entity, PopupViewOptions options = null)
        {
            var prefix = options?.Prefix ?? controller.Prefix();
            return Manager.PopupControl(controller, TypeContextUtilities.UntypedNew(entity, prefix, options?.PropertyRoute), options ?? new PopupViewOptions(prefix));
        }

        public static PartialViewResult PopupNavigate(this ControllerBase controller, IRootEntity entity, PopupNavigateOptions options = null)
        {
            var prefix = options?.Prefix ?? controller.Prefix();
            return Manager.PopupControl(controller, TypeContextUtilities.UntypedNew(entity, prefix), options ?? new PopupNavigateOptions(prefix));
        }

        public static PartialViewResult PartialView(this ControllerBase controller, TypeContext tc, string partialViewName)
        {
            return Manager.PartialView(controller, tc, partialViewName);
        }

        public static PartialViewResult PartialView(this ControllerBase controller, IRootEntity entity, string prefix, string partialViewName)
        {
            TypeContext tc = TypeContextUtilities.UntypedNew(entity, prefix);
            return Manager.PartialView(controller, tc, partialViewName);
        }

   

        public static SortedList<string, string> ToSortedList(this NameValueCollection form, string prefix)
        {
            SortedList<string, string> formValues = new SortedList<string, string>(form.Count);
            foreach (string key in form.Keys)
            {
                if (key.HasText() && (string.IsNullOrEmpty(prefix) || key.StartsWith(prefix)))
                    formValues.Add(key, form[key]);
            }
            
            return formValues;
        }

        public static SortedList<string, string> ToSortedList(this NameValueCollection form)
        {
            return form.ToSortedList(null);
        }

        public static void AddSetting(EntitySettings settings)
        {
            Navigator.Manager.EntitySettings.AddOrThrow(settings.StaticType, settings, "EntitySettings for {0} already registered");
        }

        public static void AddSettings(List<EntitySettings> settings)
        {
            Navigator.Manager.EntitySettings.AddRange(settings, s => s.StaticType, s => s, "EntitySettings");
        }

        public static EntitySettings<T> EntitySettings<T>() where T : Entity
        {
            return (EntitySettings<T>)EntitySettings(typeof(T));
        }

        public static EmbeddedEntitySettings<T> EmbeddedEntitySettings<T>() where T : EmbeddedEntity
        {
            return (EmbeddedEntitySettings<T>)EntitySettings(typeof(T));
        }

        public static EntitySettings EntitySettings(Type type)
        {
            return Manager.EntitySettings.GetOrThrow(type, "No EntitySettings for type {0} found");
        } 

        public static MappingContext UntypedApplyChanges(this ModifiableEntity entity, ControllerBase controller, string prefix = null, PropertyRoute route = null, SortedList<string, string> inputs = null)
        {
            return miApplyChanges.GetInvoker(entity.GetType()).Invoke(entity, controller, prefix, route, inputs);
        }

        static GenericInvoker<Func<ModifiableEntity, ControllerBase, string, PropertyRoute, SortedList<string, string>, MappingContext>> miApplyChanges =
            new GenericInvoker<Func<ModifiableEntity, ControllerBase, string, PropertyRoute, SortedList<string, string>, MappingContext>>((me, cc, prefix, route, dic) => ApplyChanges<TypeEntity>((TypeEntity)me, cc, prefix, route, dic));
        public static MappingContext<T> ApplyChanges<T>(this T entity, ControllerBase controller, string prefix = null, PropertyRoute route = null, SortedList<string, string> inputs = null) where T : ModifiableEntity
        {
            Mapping<T> mapping = (Mapping<T>)Navigator.EntitySettings(typeof(T)).UntypedMappingMain;

            return ApplyChanges<T>(entity, controller, mapping, prefix, route, inputs);
        }

        public static MappingContext<T> ApplyChanges<T>(this T entity, ControllerBase controller, Mapping<T> mapping, string prefix = null, PropertyRoute route = null, SortedList<string, string> inputs = null) where T : ModifiableEntity
        {
            if (prefix == null)
                prefix = controller.Prefix();

            if (inputs == null)
                inputs = controller.ControllerContext.HttpContext.Request.Form.ToSortedList(prefix);

            if (route == null)
            {
                if (!typeof(IRootEntity).IsAssignableFrom(typeof(T)))
                    throw new InvalidOperationException("In order to use ApplyChanges with EmbeddedEntities set 'route' argument");

                route = PropertyRoute.Root(typeof(T));
            }

            return Manager.ApplyChanges<T>(controller, entity, prefix, mapping, route, inputs);
        }

        public static string Prefix(this ControllerBase controller)
        {
            return controller.ControllerContext.HttpContext.Request["prefix"];
        }

        public static ModifiableEntity UntypedExtractEntity(this ControllerBase controller, string prefix = null)
        {
            return Manager.ExtractEntity(controller, prefix ?? controller.Prefix());
        }

        public static Lite<T> TryParseLite<T>(this ControllerBase controller, string requestKey)
            where T : class, IEntity
        {
            var key = controller.ControllerContext.HttpContext.Request[requestKey];
            if (key == null)
                return null;
            return Lite.Parse<T>(key);
        }

        public static Lite<T> ParseLite<T>(this ControllerBase controller, string requestKey)
            where T : class, IEntity
        {
            return Lite.Parse<T>(controller.ControllerContext.HttpContext.Request[requestKey]);
        }

        public static T ParsePercentage<T>(this ControllerBase controller, string requestKey, CultureInfo culture = null)
        {
            return (T)ReflectionTools.ParsePercentage(controller.ControllerContext.HttpContext.Request[requestKey], typeof(T), culture ?? CultureInfo.CurrentCulture);
        }

        public static T ParseValue<T>(this ControllerBase controller, string requestKey)
        {
            return ReflectionTools.Parse<T>(controller.ControllerContext.HttpContext.Request[requestKey]); 
        }

        public static T ParseValue<T>(this ControllerBase controller, string requestKey, CultureInfo culture)
        {
            return ReflectionTools.Parse<T>(controller.ControllerContext.HttpContext.Request[requestKey], culture);
        }

        public static T ExtractEntity<T>(this ControllerBase controller, string prefix = null) where T : ModifiableEntity
        {
            return (T)Manager.ExtractEntity(controller, prefix ?? controller.Prefix());
        }

        public static Lite<T> ExtractLite<T>(this ControllerBase controller, string prefix = null) where T : class, IEntity
        {
            return (Lite<T>)Manager.ExtractLite<T>(controller, prefix ?? controller.Prefix());
        }

        public static Type ResolveType(string webTypeName)
        {
            return Manager.ResolveType(webTypeName);
        }

        public static string ResolveWebTypeName(Type type)
        {
            return Manager.ResolveWebTypeName(type);
        }
     
        public static bool IsCreable(Type type, bool? isSearch = false)
        {
            return Manager.OnIsCreable(type, isSearch);
        }

        public static bool IsFindable(Type type)
        {
            return Manager.OnIsFindable(type);
        }

        public static bool IsReadOnly(Type type)
        {
            return Manager.OnIsReadOnly(type, null);
        }

        public static bool IsReadOnly(ModifiableEntity entity)
        {
            return Manager.OnIsReadOnly(entity.GetType(), entity);
        }

        public static bool IsViewable(Type type, string partialViewName)
        {
            return Manager.OnIsViewable(type, null, partialViewName);
        }

        public static bool IsViewable(ModifiableEntity entity, string partialViewName)
        {
            return Manager.OnIsViewable(entity.GetType(), entity, partialViewName);
        }

        public static bool IsNavigable(Type type, string partialViewName, bool isSearch = false)
        {
            return Manager.OnIsNavigable(type, null, partialViewName, isSearch);
        }

        public static bool IsNavigable(ModifiableEntity entity, string partialViewName, bool isSearch = false)
        {
            return Manager.OnIsNavigable(entity.GetType(), entity, partialViewName, isSearch);
        }

        public static string OnPartialViewName(ModifiableEntity entity)
        {
            return EntitySettings(entity.GetType()).OnPartialViewName(entity); 
        }

        public static void RegisterArea(Type clientType, 
            string areaName = null, 
            string controllerNamespace = null, 
            string resourcesNamespace = null)
        {
            if (areaName == null)
                areaName = clientType.Namespace.AfterLast('.');

            if (areaName.Start(1) == "/")
                throw new SystemException("Invalid start character / in {0}".FormatWith(areaName));

            if (controllerNamespace == null)
                controllerNamespace = clientType.Namespace;

            if (resourcesNamespace == null)
                resourcesNamespace = clientType.Namespace;

            var assembly = clientType.Assembly;

            CompiledViews.RegisterArea(assembly, areaName);
            SignumControllerFactory.RegisterControllersIn(assembly, controllerNamespace, areaName);

            EmbeddedFilesRepository rep = new EmbeddedFilesRepository(assembly, "~/" + areaName + "/", resourcesNamespace);
            if (!rep.IsEmpty)
                FileRepositoryManager.Register(rep);
        }

        public static void Initialize()
        {
            Manager.Initialize();
            Finder.Manager.Initialize();
        }

        internal static void AssertNotReadonly(Entity ident)
        {
            if (Navigator.IsReadOnly(ident))
                throw new UnauthorizedAccessException("{0} is read-only".FormatWith(ident));
        }
    }
    
    public class NavigationManager
    {
        public Dictionary<Type, EntitySettings> EntitySettings {get;set;}

        public static string ViewPrefix = "~/Signum/Views/{0}.cshtml";

        public string NormalPageView = ViewPrefix.FormatWith("NormalPage");
        public string NormalControlView = ViewPrefix.FormatWith("NormalControl");
        public string PopupControlView = ViewPrefix.FormatWith("PopupControl");
        public string ValueLineBoxView = ViewPrefix.FormatWith("ValueLineBox");
        
        protected Dictionary<string, Type> WebTypeNames { get; private set; }
      
        public NavigationManager(string entityStateKeyToHash)
            : this(new MD5CryptoServiceProvider().Using(p => p.ComputeHash(UTF8Encoding.UTF8.GetBytes(entityStateKeyToHash))))
        {
        }

        public NavigationManager(byte[] entityStateKey)
        {
            EntitySettings = new Dictionary<Type, EntitySettings>();
            EntityStateKey = entityStateKey;

            formatter = new NetDataContractSerializer();

            var ss = new SurrogateSelector();
            ss.AddSurrogate(typeof(SqlHierarchyId),
                new StreamingContext(StreamingContextStates.All),
                new SqlHierarchySurrogate());

            formatter.SurrogateSelector = ss;
        }

        class SqlHierarchySurrogate : ISerializationSurrogate 
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                SqlHierarchyId shi = (SqlHierarchyId)obj;
                info.AddValue("route", shi.ToString());
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                return SqlHierarchyId.Parse(info.GetString("route"));
            }
        }
        
        public event Action Initializing;
        public bool Initialized { get; private set; }
        internal void Initialize()
        {
            if (!Initialized)
            {
                foreach (var es in EntitySettings.Values)
                {
                    if (string.IsNullOrEmpty(es.WebTypeName) && !es.StaticType.IsEmbeddedEntity())
                        es.WebTypeName = TypeLogic.TypeToName.TryGetC(es.StaticType) ?? Reflector.CleanTypeName(es.StaticType);
                }

                WebTypeNames = EntitySettings.Values.Where(es => es.WebTypeName.HasText())
                    .ToDictionaryEx(es => es.WebTypeName, es => es.StaticType, StringComparer.InvariantCultureIgnoreCase, "WebTypeNames");


                Navigator.RegisterArea(typeof(Navigator), areaName: "Signum", resourcesNamespace: "Signum.Web.Signum");
                FileRepositoryManager.Register(new LocalizedJavaScriptRepository(typeof(JavascriptMessage), "Signum"));
                FileRepositoryManager.Register(new CalendarLocalizedJavaScriptRepository("~/Signum/calendarResources/"));
                FileRepositoryManager.Register(new UrlsRepository("~/Signum/urls/"));



                Schema.Current.ApplicationName = System.Web.Hosting.HostingEnvironment.ApplicationHost.GetPhysicalPath();

                if (Initializing != null)
                    Initializing();

                Initialized = true;
            }
        }

        HashSet<string> loadedModules = new HashSet<string>();

        public bool NotDefined(MethodBase currentMethod)
        {
            string methodName = currentMethod.DeclaringType.TypeName() + "." + currentMethod.Name;

            return loadedModules.Add(methodName);
        }

        public void AssertDefined(MethodBase methodBase)
        {
            string name = methodBase.DeclaringType.TypeName() + "." + methodBase.Name;

            if (!loadedModules.Contains(name))
                throw new InvalidOperationException("Call {0} first".FormatWith(name));
        }

        protected internal string GetOrCreateTabID(ControllerBase c)
        {
            if (c.ControllerContext.HttpContext.Request.Form.AllKeys.Contains(ViewDataKeys.TabId))
            {
                string tabID = c.ControllerContext.HttpContext.Request.Form[ViewDataKeys.TabId];
                if (tabID.HasText())
                    return tabID;
            }
            return Guid.NewGuid().ToString();
        }

        protected internal virtual ViewResult NormalPage(ControllerBase controller, IRootEntity rootEntity, NavigateOptions options)
        {
            FillViewDataForViewing(controller, rootEntity, options);

            return new ViewResult()
            {
                ViewName = NormalPageView,
                MasterName = null,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual PartialViewResult NormalControl(ControllerBase controller, IRootEntity rootEntity, NavigateOptions options)
        {
            FillViewDataForViewing(controller, rootEntity, options);

            return new PartialViewResult()
            {
                ViewName = NormalControlView,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        public void FillViewDataForViewing(ControllerBase controller, IRootEntity rootEntity, NavigateOptions options)
        {
            TypeContext tc = TypeContextUtilities.UntypedNew(rootEntity, "");
            controller.ViewData.Model = tc;

            var entity = (ModifiableEntity)rootEntity;

            controller.ViewData[ViewDataKeys.PartialViewName] = options.PartialViewName ?? Navigator.OnPartialViewName(entity);
            tc.ViewOverrides = Navigator.EntitySettings(entity.GetType()).GetViewOverrides();

            if (controller.ViewData[ViewDataKeys.TabId] == null)
                controller.ViewData[ViewDataKeys.TabId] = GetOrCreateTabID(controller);

            controller.ViewData[ViewDataKeys.ShowOperations] = options.ShowOperations;

            controller.ViewData[ViewDataKeys.WriteEntityState] = options.WriteEntityState || (bool?)controller.ViewData[ViewDataKeys.WriteEntityState] == true;

            AssertViewableEntitySettings(entity);

            tc.ReadOnly = options.ReadOnly ?? Navigator.IsReadOnly(entity);
        }

        public string GetTypeTitle(ModifiableEntity mod)
        {
            if (mod == null)
                return "";

            string niceName = mod.GetType().NiceName();

            Entity ident = mod as Entity;
            if (ident == null)
                return niceName;

            if (ident.IsNew)
            {
                return LiteMessage.New_G.NiceToString().ForGenderAndNumber(ident.GetType().GetGender()) + " " + niceName;
            }
            return niceName + " " + ident.Id;
        }

        protected internal virtual PartialViewResult PopupControl(ControllerBase controller, TypeContext typeContext, PopupOptionsBase popupOptions)
        {
            Type cleanType = typeContext.UntypedValue.GetType();

            ModifiableEntity entity = (ModifiableEntity)typeContext.UntypedValue;
            AssertViewableEntitySettings(entity);
            
            controller.ViewData.Model = typeContext;
            controller.ViewData[ViewDataKeys.PartialViewName] = popupOptions.PartialViewName ?? Navigator.OnPartialViewName(entity);
            typeContext.ViewOverrides = Navigator.EntitySettings(entity.GetType()).GetViewOverrides();

            bool isReadOnly = popupOptions.ReadOnly ?? Navigator.IsReadOnly(entity);
            if (isReadOnly)
                typeContext.ReadOnly = true;

            ViewMode mode = popupOptions.ViewMode;
            controller.ViewData[ViewDataKeys.ViewMode] = mode;
            controller.ViewData[ViewDataKeys.ShowOperations] = popupOptions.ShowOperations;
            if (mode == ViewMode.View)
            {
                controller.ViewData[ViewDataKeys.RequiresSaveOperation] = ((PopupViewOptions)popupOptions).RequiresSaveOperation ?? 
                    (entity is Entity && EntityKindCache.RequiresSaveOperation(entity.GetType()));
            }

            return new PartialViewResult
            {
                ViewName = PopupControlView,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

        protected internal virtual PartialViewResult PartialView(ControllerBase controller, TypeContext tc, string partialViewName)
        {
            TypeContext cleanTC = TypeContextUtilities.CleanTypeContext(tc);
            Type cleanType = cleanTC.UntypedValue.GetType();

            if (!Navigator.IsViewable(cleanType, partialViewName))
                throw new Exception(NormalControlMessage.ViewForType0IsNotAllowed.NiceToString().FormatWith(cleanType.Name));

            controller.ViewData.Model = cleanTC;

            if (Navigator.IsReadOnly(cleanType))
                cleanTC.ReadOnly = true;

            cleanTC.ViewOverrides = Navigator.EntitySettings(cleanType).GetViewOverrides();

            return new PartialViewResult
            {
                ViewName = partialViewName ?? Navigator.OnPartialViewName((ModifiableEntity)cleanTC.UntypedValue),
                ViewData = controller.ViewData,
                TempData = controller.TempData
            };
        }

       

        protected internal virtual Type ResolveType(string webTypeName)
        {
            return WebTypeNames.TryGetC(webTypeName) ?? TypeLogic.NameToType.GetOrThrow(webTypeName, "webTypeName {0} not found");
        }

        protected internal virtual string ResolveWebTypeName(Type type)
        {
            var es = EntitySettings.TryGetC(type);
            if (es != null)
                return es.WebTypeName;

            if (type.IsEntity())
            {
                var cleanName = TypeLogic.TryGetCleanName(type);
                if (cleanName != null)
                    return cleanName;
            }

            throw new InvalidOperationException("Impossible to resolve WebTypeName for '{0}' because is not registered in Navigator's EntitySettings".FormatWith(type.Name) + 
                (type.IsEntity() ? " or the Schema" : null));
        }

        protected internal virtual MappingContext<T> ApplyChanges<T>(ControllerBase controller, T entity, string prefix, Mapping<T> mapping, PropertyRoute route, SortedList<string, string> inputs) where T: ModifiableEntity
        {
            using (HeavyProfiler.Log("ApplyChanges", () => typeof(T).TypeName()))
            using (new EntityCache(EntityCacheType.Normal))
            {
                EntityCache.AddFullGraph((ModifiableEntity)entity);

                RootContext<T> ctx = new RootContext<T>(prefix, inputs, route, controller) { Value = entity };
                mapping(ctx);
                return ctx;
            }
        }

        protected internal virtual ModifiableEntity ExtractEntity(ControllerBase controller, string prefix)
        {
            NameValueCollection form = controller.ControllerContext.HttpContext.Request.Form;

            var state = form[TypeContextUtilities.Compose(prefix, EntityBaseKeys.EntityState)];

            if (state.HasText())
                return Navigator.Manager.DeserializeEntity(state);


            var key = TypeContextUtilities.Compose(prefix, EntityBaseKeys.RuntimeInfo);

            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(form[key]);

            if (runtimeInfo == null)
                throw new ArgumentNullException("{0} not found in form request".FormatWith(key));

            if (runtimeInfo.EntityType.IsEntity() && !runtimeInfo.IsNew)
                return Database.Retrieve(runtimeInfo.EntityType, runtimeInfo.IdOrNull.Value);
            else
                return new ConstructorContext(controller).ConstructUntyped(runtimeInfo.EntityType);
        }

        protected internal virtual Lite<T> ExtractLite<T>(ControllerBase controller, string prefix)
            where T:class, IEntity
        {
            NameValueCollection form = controller.ControllerContext.HttpContext.Request.Form;
            RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(form[TypeContextUtilities.Compose(prefix, EntityBaseKeys.RuntimeInfo)]);

            if (!runtimeInfo.IdOrNull.HasValue)
                throw new ArgumentException("Could not create a Lite without an Id");

            return (Lite<T>)runtimeInfo.ToLite();
        }

        public event Func<Type, bool> IsCreable;

        internal protected virtual bool OnIsCreable(Type type, bool? isSearch)
        {
            EntitySettings es = EntitySettings.TryGetC(type);
            if (es == null)
                return true;

            if (isSearch.HasValue && !es.OnIsCreable(isSearch.Value))
                return false;


            if (IsCreable != null)
                foreach (var isCreable in IsCreable.GetInvocationListTyped())
                {
                    if (!isCreable(type))
                        return false;
                }

            return true;
        }
        
        internal protected virtual bool OnIsFindable(Type type)
        {
            if(!Finder.IsFindable(type))
                return false;

            EntitySettings es = EntitySettings.TryGetC(type);
            if (es != null && !es.OnIsFindable())
                return false;

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
            if (IsViewable != null)
            {
                foreach (var isViewable in IsViewable.GetInvocationListTyped())
                {
                    if (!isViewable(type, entity))
                        return false;
                }
            }

            return true;
        }

        internal protected virtual EntitySettings AssertViewableEntitySettings(ModifiableEntity entity)
        {
            EntitySettings es = EntitySettings.TryGetC(entity.GetType());
            if (es == null)
                throw new InvalidOperationException("No EntitySettings for type {0}".FormatWith(entity.GetType().Name));

            if (es.OnPartialViewName(entity) == null)
                throw new InvalidOperationException("No view has been set in the EntitySettings for {0}".FormatWith(entity.GetType().Name));

            if (!IsViewableBase(entity.GetType(), entity))
                throw new InvalidOperationException("Entities of type {0} are not viewable".FormatWith(entity.GetType().Name));

            return es;
        }

        internal protected virtual bool OnIsNavigable(Type type, ModifiableEntity entity, string partialViewName, bool isSearch)
        {
            EntitySettings es = EntitySettings.TryGetC(type);

            return es != null &&
                IsViewableBase(type, entity) &&
                es.OnIsNavigable(partialViewName, isSearch);
        }

        internal protected virtual bool OnIsViewable(Type type, ModifiableEntity entity, string partialViewName)
        {
            EntitySettings es = EntitySettings.TryGetC(type);

            return es != null &&
                IsViewableBase(type, entity) &&
                es.OnIsViewable(partialViewName);
        }

      

        public byte[] EntityStateKey;

        public string SerializeEntity(ModifiableEntity entity)
        {
            using (HeavyProfiler.LogNoStackTrace("SerializeEntity"))
            {
                var array = new MemoryStream().Using(ms =>
                {
                    using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress))
                        formatter.Serialize(ds, entity);
                    return ms.ToArray();
                });

                if (EntityStateKey != null)
                    array = Encrypt(array);

                return Convert.ToBase64String(array); 
            }
        }

        public IFormatter Formatter { get { return formatter; } }

        IFormatter formatter;

        public ModifiableEntity DeserializeEntity(string viewState)
        {
            using (HeavyProfiler.LogNoStackTrace("DeserializeEntity"))
            {
                var array = Convert.FromBase64String(viewState);

                if (EntityStateKey != null)
                    array = Decrypt(array);

                using (var ms = new MemoryStream(array))
                using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                    return (ModifiableEntity)formatter.Deserialize(ds);
            }
        }

        //http://stackoverflow.com/questions/8041451/good-aes-initialization-vector-practice
        byte[] Encrypt(byte[] toEncryptBytes)
        {
            using (var provider = new AesCryptoServiceProvider())
            {
                provider.Key = EntityStateKey;
                provider.Mode = CipherMode.CBC;
                provider.Padding = PaddingMode.PKCS7;
                using (var encryptor = provider.CreateEncryptor(provider.Key, provider.IV))
                {
                    using (var ms = new MemoryStream())
                    {
                        ms.Write(provider.IV, 0, provider.IV.Length);
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(toEncryptBytes, 0, toEncryptBytes.Length);
                            cs.FlushFinalBlock();
                        }
                        return ms.ToArray();
                    }
                }
            }
        }

        byte[] Decrypt(byte[] encryptedString)
        {
            using (var provider = new AesCryptoServiceProvider())
            {
                provider.Key = EntityStateKey;
                using (var ms = new MemoryStream(encryptedString))
                {
                    // Read the first 16 bytes which is the IV.
                    byte[] iv = new byte[16];
                    ms.Read(iv, 0, 16);
                    provider.IV = iv;

                    using (var decryptor = provider.CreateDecryptor())
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            return cs.ReadAllBytes();
                        }
                    }
                }
            }
        }


        public event Func<Lite<Entity>, IDisposable> RetrievingForView; 
        public IDisposable OnRetrievingForView(Lite<Entity> lite)
        {
            return Disposable.Combine(RetrievingForView, f => f(lite));
        }
    }

    public enum JsonResultType
    {
        url,
        ModelState,
        messageBox
    }

    public static class JsonAction
    {
        public static ActionResult RedirectHttpOrAjax(this ControllerBase controller,string url)
        {
            if (controller.ControllerContext.HttpContext.Request.IsAjaxRequest())
                return RedirectAjax(url);
            else
                return new RedirectResult(url);
        }

        public static JsonNetResult RedirectAjax(string url)
        {
            return new JsonNetResult(new
            {
                result = JsonResultType.url.ToString(),
                url = url
            });
        }

        public static JsonNetResult ToJsonModelState(this ModelStateDictionary dictionary)
        {
            return ToJsonModelState(dictionary, null, null);
        }

        public static JsonNetResult ToJsonModelState(this ModelStateDictionary dictionary, string newToString, string newToStringLink)
        {
            Dictionary<string, object> result = new Dictionary<string, object>
            {
                {"result", JsonResultType.ModelState.ToString()},
                {"ModelState", dictionary.ToDictionary(kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray())
                }
            };

            if (newToString != null)
                result.Add(EntityBaseKeys.ToStr, newToString);
            if (newToStringLink != null)
                result.Add(EntityBaseKeys.Link, newToStringLink);

            return new JsonNetResult { Data = result };
        }

    }

    public class JsonNetResult : ActionResult
    {
        public Encoding ContentEncoding { get; set; }
        public string ContentType { get; set; }
        public object Data { get; set; }

        public JsonSerializerSettings SerializerSettings { get; set; }
        public Formatting Formatting { get; set; }

        public JsonNetResult()
        {
            SerializerSettings = new JsonSerializerSettings();
        }

        public JsonNetResult(object data)
        {
            Data = data;
            SerializerSettings = new JsonSerializerSettings();
        }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            HttpResponseBase response = context.HttpContext.Response;

            response.ContentType = !string.IsNullOrEmpty(ContentType)
              ? ContentType
              : "application/json";

            if (ContentEncoding != null)
                response.ContentEncoding = ContentEncoding;

            if (Data != null)
            {
                JsonTextWriter writer = new JsonTextWriter(response.Output) { Formatting = Formatting };

                JsonSerializer serializer = JsonSerializer.Create(SerializerSettings);
                serializer.Serialize(writer, Data);

                writer.Flush();
            }
        }
    }
}
