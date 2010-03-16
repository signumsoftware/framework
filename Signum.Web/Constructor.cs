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

        public static object Construct(Type type, Controller controller)
        {
            return ConstructorManager.Construct(type, controller);
        }

        public static T Construct<T>(Controller controller)
        {
            return (T)Construct(typeof(T), controller);
        }

        public static ModifiableEntity ConstructStrict(Type type)
        {
            return ConstructorManager.ConstructStrict(type);
        }

        public static T ConstructStrict<T>() where T : ModifiableEntity
        {
            return (T)ConstructStrict(typeof(T));
        }

    }
    
    public class ConstructorManager
    {
        public event Func<Type, Controller, object> GeneralConstructor;

        public Dictionary<Type, Func<Controller, object>> Constructors;

        internal void Initialize()
        {
            if (Constructors == null)
                Constructors = new Dictionary<Type, Func<Controller, object>>();
        }

        public virtual object Construct(Type type, Controller controller)
        {
            Func<Controller, object> c = Constructors.TryGetC(type);
            if (c != null)
            {
                object result = c(controller);
                if (result != null)
                    return result;
            }

            if (GeneralConstructor != null)
            {
                object result = GeneralConstructor(type, controller);
                if (result != null)
                    return result;
            }

            return DefaultContructor(type, controller);
        }

        public virtual ModifiableEntity ConstructStrict(Type type)
        {
            Func<Controller, object> c = Constructors.TryGetC(type);
            if (c != null)
            {
                object result = c(null);
                if (result != null)
                    return (ModifiableEntity)result;
            }

            return (ModifiableEntity)DefaultContructor(type, null);
        }

        public static object DefaultContructor(Type type, Controller controller)
        {
            object result = Activator.CreateInstance(type);

            if (controller != null)
                result = AddFilterProperties(result, controller);

            return result;
        }

        public static object AddFilterProperties(object obj, Controller controller)
        {
            if (obj == null)
                throw new ArgumentNullException("result");

            Type type = obj.GetType();

            object queryName = Navigator.ResolveQueryFromUrlName(controller.Request.Params["sfQueryUrlName"]);

            var filters = FindOptionsModelBinder.ExtractFilterOptions(controller.HttpContext, queryName)
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
