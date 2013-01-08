using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Windows;
using Signum.Entities;
using Signum.Entities.Basics;
using System.Reflection;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;

namespace Signum.Windows
{
    public static class Constructor
    {
        public static ConstructorManager Manager;

        public static void Start(ConstructorManager constructorManager)
        {
            Manager = constructorManager;
            constructorManager.Initialize();
        }

        public static object Construct(Type type, FrameworkElement element)
        {
            return Manager.Construct(type, element); 
        }

        public static T Construct<T>(FrameworkElement element)
        {
            return (T)Construct(typeof(T), element);
        }

        public static T DefaultConstruct<T>(FrameworkElement element)
        {
            return (T)Construct(typeof(T), element);
        }

        public static void Register<T>(Func<FrameworkElement, T> constructor)
            where T : class
        {
            Manager.Constructors.Add(typeof(T), constructor);
        }
    }
    
    public class ConstructorManager
    {
        public event Func<Type, FrameworkElement, object> GeneralConstructor;

        public Dictionary<Type, Func<FrameworkElement, object>> Constructors;

        internal void Initialize()
        {
            if (Constructors == null)
                Constructors = new Dictionary<Type, Func<FrameworkElement, object>>();
        }

        public virtual object Construct(Type type, FrameworkElement element)
        {
            Func<FrameworkElement, object> c = Constructors.TryGetC(type);
            if (c != null)
            {
                object result = c(element);
                return result;
            }

            if (GeneralConstructor != null)
            {
                object result = GeneralConstructor(type, element);
                if (result != null)
                    return result;
            }

            return DefaultContructor(type, element);
        }

        protected internal virtual object DefaultContructor(Type type, FrameworkElement element)
        {
            object result = Activator.CreateInstance(type);

            AddFilterProperties(element, result);

            return result;
        }

        protected internal virtual object AddFilterProperties(FrameworkElement element, object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("result");

            Type type = obj.GetType();

            if (element is SearchControl)
            {
                var filters = ((SearchControl)element).FilterOptions.Where(fo => fo.Operation == FilterOperation.EqualTo && fo.Token is ColumnToken);

                var pairs = from pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            join fo in filters on pi.Name equals fo.Token.Key
                            where Server.CanConvert(fo.Value, pi.PropertyType) && fo.Value != null
                            select new { pi, fo };

                foreach (var p in pairs)
                {
                    p.pi.SetValue(obj, Server.Convert(p.fo.Value, p.pi.PropertyType), null);
                }
            }

            return obj; 
        }
    }
}
