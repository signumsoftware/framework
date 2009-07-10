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

        internal static bool IsFindable(Type type)
        {
            return Manager.QuerySetting.ContainsKey(type); 
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

        public static string TypeName(Type t)
        {
            return Manager.ServerTypes.TryGetC(t).TryCC(a => a.FriendlyName) ?? t.FriendlyName();
        }
    }


    public class NavigationManager
    {
        public Dictionary<Type, EntitySettings> Settings{get;set;}
        public Dictionary<object, QuerySetting> QuerySetting{get;set;}
        public Dictionary<Type, TypeDN> ServerTypes{get;private set;}

        internal void Initialize()
        {
            if (Settings == null)
                Settings = new Dictionary<Type, EntitySettings>();

            var dic = Server.Service<IQueryServer>().GetQueryNames().ToDictionary(a => a, a => new QuerySetting());
            if (QuerySetting != null)
                dic.SetRange(QuerySetting);
            QuerySetting = dic;

            ServerTypes = Server.Service<IBaseServer>().ServerTypes(); 
        }


        public virtual string SearchTitle(object queryName)
        {
            if (QuerySetting != null)
            {
                QuerySetting vs = QuerySetting.TryGetC(queryName);
                if (vs != null && vs.Title != null)
                    return vs.Title; 
            }

            string title = (queryName is Type) ? Navigator.TypeName(((Type)queryName)) :
                           (queryName is Enum) ? EnumExtensions.NiceToString((Enum)queryName) :
                            queryName.ToString();

            return Resources.FinderOf0.Formato(title);
        }

        public virtual object Find(FindOptions findOptions)
        {
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

            sw.Icon = EntitySettings.GetIcon((findOptions.QueryName as Type).TryCC(t => Settings.TryGetC(t)), EntitySettings.WindowsType.Find);

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


            win.Icon = EntitySettings.GetIcon(es, EntitySettings.WindowsType.View);

            Common.SetIsReadOnly(win, es.IsReadOnly(viewOptions.Admin)); 

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

        public virtual void Admin(AdminOptions adminOptions)
        {
            Type type = adminOptions.Type;

            EntitySettings es = Settings.GetOrThrow(type, Resources.NoEntitySettingsForType0);

            AdminWindow nw = new AdminWindow(type) 
            { 
                MainControl = es.View.ThrowIfNullC(Resources.NoAdminControlFor0.Formato(type))(), 
                Icon = EntitySettings.GetIcon(es, EntitySettings.WindowsType.Admin)
            
            };
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
            return type.FollowC(t => t.BaseType).Select(t => Settings.TryGetC(t)).NotNull().FirstOrDefault();
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
    }
}
