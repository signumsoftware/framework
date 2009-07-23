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

namespace Signum.Windows
{
    public static class Navigator
    {
        public static NavigationManager Manager {get; private set;}

        public static void Start(NavigationManager navigator)
        {
            navigator.Initialize();

            Manager = navigator;
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
                SearchOnLoad = true,
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

            ViewButtons vb = lazy != null && (lazy.UntypedEntityOrNull == null || !lazy.UntypedEntityOrNull.IsNew) ? ViewButtons.Save : ViewButtons.OkCancel;

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

        internal static EntitySettings FindSettings(Type type)
        {
            return Manager.FindSettings(type);
        }



        public static DataTemplate FindDataTemplate(FrameworkElement element, Type entityType)
        {
            DataTemplate template = FindSettings(entityType).TryCC(ess => ess.DataTemplate);
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

        public static Type SelectType(Type[] implementations)
        {
            return Manager.SelectTypes(implementations);
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
        public Dictionary<Type, EntitySettings> Settings{get;set;}
        public Dictionary<object, QuerySettings> QuerySetting{get;set;}
        public Dictionary<Type, TypeDN> ServerTypes{get;private set;}

        public event Func<Type, bool> GlobalIsCreable;
        public event Func<Type, bool> GlobalIsReadOnly;
        public event Func<Type, bool> GlobalIsViewable;
        public event Func<object, bool> GlobalIsFindable;

        public event Action<Window, EntitySettings, WindowsType> TaskViewWindow; 

        public NavigationManager()
        {
            TaskViewWindow += TaskSetSetIcon;
            TaskViewWindow += TaskSetLabelShortcuts;
        }

        public static void TaskSetSetIcon(Window windows, EntitySettings es, WindowsType winType)
        {
            windows.Icon = EntitySettings.GetIcon(es, WindowsType.View);
        }

        public static void TaskSetLabelShortcuts(Window windows, EntitySettings es, WindowsType winType)
        {
            if (winType == WindowsType.Find)
                return;

            ShortcutHelper.SetLabelShortcuts(windows);
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
            QuerySettings vs = QuerySetting.TryGetC(queryName);
            if (vs != null && vs.Title != null)
                return vs.Title; 

            string title = (queryName is Type) ? ((Type)queryName).TypeName() :
                           (queryName is Enum) ? EnumExtensions.NiceToString((Enum)queryName) :
                            queryName.ToString();

            return Resources.FinderOf0.Formato(title);
        }

        public virtual object Find(FindOptions findOptions)
        {
            if (!IsFindable(findOptions.QueryName))
                throw new ApplicationException("The query {0} is not allowed".Formato(findOptions.QueryName)); 

            SearchWindow sw = new SearchWindow()
            {
                QueryName = findOptions.QueryName,
                Buttons = findOptions.Buttons,
                MultiSelection = findOptions.AllowMultiple,
                FilterOptions = new FreezableCollection<FilterOptions>(findOptions.FilterOptions),
                SearchOnLoad = findOptions.SearchOnLoad,
                Mode = findOptions.FilterMode,
                Title = SearchTitle(findOptions.QueryName),
            };

            EntitySettings es = (findOptions.QueryName as Type).TryCC(t => Settings.TryGetC(t)); 
            if(TaskViewWindow != null)
                TaskViewWindow(sw, es, WindowsType.Find);

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

                entity = viewOptions.Buttons == ViewButtons.Save ? Server.RetrieveLazyThin((Lazy)entity) :
                                                                   Server.RetrieveLazyFat((Lazy)entity);
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

                NormalWindow nw = new NormalWindow() { Buttons = viewOptions.Buttons, MainControl = ctrl};

                win = nw;
            }

            if (IsReadOnly(entity.GetType(), viewOptions.Admin))
                Common.SetIsReadOnly(win, true);

            if (TaskViewWindow != null)
                TaskViewWindow(win, es, WindowsType.View);

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
            if(es == null)
                return true; 
            
            return es.IsCreable(admin);
        }

        internal protected virtual bool IsReadOnly(Type type, bool admin)
        {
            if (GlobalIsReadOnly != null && !GlobalIsReadOnly(type))
                return false;

            EntitySettings es = Settings.TryGetC(type);
            if (es == null)
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

            return es.IsViewable(admin);
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
                TaskViewWindow(nw, es, WindowsType.Admin);

            nw.Show();
        }

        public virtual Type SelectTypes(Type[] implementations)
        {
            if (implementations == null || implementations.Length == 0)
                throw new ArgumentException("implementations");

            if (implementations.Length == 1)
                return implementations[0];

            TypeSelectorWindow win = new TypeSelectorWindow();
            win.Types = implementations;
            if (win.ShowDialog() != true)
                return null;

            return win.SelectedType;
        }


        public EntitySettings FindSettings(Type type)
        {
            return Settings.TryGetC(type);
        }

        HashSet<string> loadedModules = new HashSet<string>();

        public bool NotDefined<T>()
        {
            return NotDefined(typeof(T).FullName);
        }

        public bool NotDefined(string moduleName)
        {
            return loadedModules.Add(moduleName);
        }

        public bool ContainsDefinition(string moduleName)
        {
            return loadedModules.Contains(moduleName); 
        }

        public bool ContainsDefinition<T>()
        {
            return loadedModules.Contains(typeof(T).FullName);
        }
    }
}
