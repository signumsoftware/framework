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

namespace Signum.Web
{
    public static class Constructor
    {
        public static ConstructorManager ConstructorManager;

        public static void Start(ConstructorManager constructorManager)
        {
            ConstructorManager = constructorManager;
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

            return DefaultContructor(type);
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

            return (ModifiableEntity)DefaultContructor(type);
        }

        public static object DefaultContructor(Type type)
        {
            object result = Activator.CreateInstance(type);

            return result;
        }
    }
}
