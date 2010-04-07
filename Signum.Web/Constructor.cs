using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Web;
using Signum.Entities;
using Signum.Entities.Basics;
using System.Reflection;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using System.Web.Mvc;
using Signum.Engine;

namespace Signum.Web
{
    public static class Constructor
    {
        public static ConstructorManager ConstructorManager;

        public static void Start(ConstructorManager constructorManager)
        {
            ConstructorManager = constructorManager;
            constructorManager.Initialize();
        }

        public static ModifiableEntity Construct(Type type)
        {
            return ConstructorManager.Construct(type);
        }

        public static T Construct<T>() where T : ModifiableEntity
        {
            return (T)ConstructorManager.Construct(typeof(T));
        }

        public static object VisualConstruct(Type type, ControllerBase controller)
        {
            return ConstructorManager.VisualConstruct(type, controller);
        }

        public static T VisualConstruct<T>(ControllerBase controller) where T : ModifiableEntity
        {
            return (T)ConstructorManager.VisualConstruct(typeof(T), controller);
        }

    }
    
    public class ConstructorManager
    {
        public event Func<Type, ModifiableEntity> GeneralConstructor;
        public Dictionary<Type, Func<ModifiableEntity>> Constructors;

        public event Func<Type, ControllerBase, object> VisualGeneralConstructor;
        public Dictionary<Type, Func<ControllerBase, object>> VisualConstructors;

        internal void Initialize()
        {
            if (Constructors == null)
                Constructors = new Dictionary<Type, Func<ModifiableEntity>>();
            if (VisualConstructors == null)
                VisualConstructors = new Dictionary<Type, Func<ControllerBase, object>>();
        }

        public virtual ModifiableEntity Construct(Type type)
        {
            Func<ModifiableEntity> c = Constructors.TryGetC(type);
            if (c != null)
            {
                ModifiableEntity result = c();
                if (result != null)
                    return result;
            }

            if (GeneralConstructor != null)
            {
                ModifiableEntity result = GeneralConstructor(type);
                if (result != null)
                    return result;
            }

            return DefaultContructor(type);
        }

        public static ModifiableEntity DefaultContructor(Type type)
        {
            return (ModifiableEntity)Activator.CreateInstance(type);
        }

        public virtual object VisualConstruct(Type type, ControllerBase controller)
        {
            Func<ControllerBase, object> c = VisualConstructors.TryGetC(type);
            if (c != null)
            {
                object result = c(controller);
                if (result != null)
                    return result;
            }

            if (VisualGeneralConstructor != null)
            {
                object result = VisualGeneralConstructor(type, controller);
                if (result != null)
                    return result;
            }

            object o = Constructor.Construct(type);
            return AddFilterProperties(o, controller);
        }

        public static object AddFilterProperties(object obj, ControllerBase controller)
        {
            if (obj == null)
                throw new ArgumentNullException("result");

            HttpContextBase httpContext = controller.ControllerContext.HttpContext;

            if (!httpContext.Request.Params.AllKeys.Contains("sfQueryUrlName"))
                return obj;

            Type type = obj.GetType();

            object queryName = Navigator.ResolveQueryFromUrlName(httpContext.Request.Params["sfQueryUrlName"]);

            var filters = FindOptionsModelBinder.ExtractFilterOptions(httpContext, queryName)
                .Where(fo => fo.Operation == FilterOperation.EqualTo);

            var pairs = from pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        join fo in filters on pi.Name equals fo.Token.Key
                        //where CanConvert(fo.Value, pi.PropertyType) && fo.Value != null
                        where fo.Value != null
                        select new { pi, fo };

            foreach (var p in pairs)
                p.pi.SetValue(obj, Convert(p.fo.Value, p.pi.PropertyType), null);
            
            return obj;
        }

        public static object Convert(object obj, Type type)
        {
            if (obj == null) return null;

            Type objType = obj.GetType();

            if (type.IsAssignableFrom(objType))
                return obj;

            if (typeof(Lite).IsAssignableFrom(objType) && type.IsAssignableFrom(((Lite)obj).RuntimeType))
            {
                Lite lite = (Lite)obj;
                return lite.UntypedEntityOrNull ?? Database.RetrieveAndForget(lite);
            }

            if (typeof(Lite).IsAssignableFrom(type))
            {
                Type liteType = Reflector.ExtractLite(type);

                if (typeof(Lite).IsAssignableFrom(objType))
                {
                    Lite lite = (Lite)obj;
                    if (liteType.IsAssignableFrom(lite.RuntimeType))
                    {
                        if (lite.UntypedEntityOrNull != null)
                            return Lite.Create(liteType, lite.UntypedEntityOrNull);
                        else
                            return Lite.Create(liteType, lite.Id, lite.RuntimeType, lite.ToStr);
                    }
                }

                else if (liteType.IsAssignableFrom(objType))
                {
                    return Lite.Create(liteType, (IdentifiableEntity)obj);
                }
            }

            throw new InvalidCastException(Properties.Resources.ImposibleConvertObject0From1To2.Formato(obj, objType, type));
        }
    }
}
