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
        public static ConstructorManager ConstructorManager;

        public static void Start(ConstructorManager constructorManager)
        {
            ConstructorManager = constructorManager;
            constructorManager.Initialize();
        }

        public static object Construct(Type type, Window window)
        {
            return ConstructorManager.Construct(type, window); 
        }

        public static T Construct<T>(Window window)
        {
            return (T)Construct(typeof(T), window);
        }

        public static void Register<T>(Func<Window, T> constructor) 
            where T:class
        {
            ConstructorManager.Constructors.Add(typeof(T), constructor);
        }
    }
    
    public class ConstructorManager
    {
        public event Func<Type, Window, object> GeneralConstructor; 

        public Dictionary<Type, Func<Window, object>> Constructors;

        internal void Initialize()
        {
            if (Constructors == null)
                Constructors = new Dictionary<Type, Func<Window, object>>();
        }

        public virtual object Construct(Type type, Window window)
        {
            Func<Window, object> c = Constructors.TryGetC(type);
            if (c != null)
            {
                object result = c(window);
                return result;
            }

            if (GeneralConstructor != null)
            {
                object result = GeneralConstructor(type, window);
                if (result != null)
                    return result;
            }

            return DefaultContructor(type, window);
        }

        public static object DefaultContructor(Type type, Window window)
        {
            object result = Activator.CreateInstance(type);

            AddFilterProperties(window, result);

            return result;
        }

        public static object AddFilterProperties(Window window, object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("result");

            Type type = obj.GetType();

            if (window is SearchWindow)
            {
                var filters = ((SearchWindow)window).FilterOptions.Where(fo => fo.Operation == FilterOperation.EqualTo && fo.Token is ColumnToken);

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
