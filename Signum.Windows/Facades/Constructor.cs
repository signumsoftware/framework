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

        public static void Start(ConstructorManager manager)
        {
            Manager = manager;
        }

        public static T Construct<T>(FrameworkElement element = null, List<object> args = null)
         where T : ModifiableEntity
        {
            return (T)Construct(typeof(T), element);
        }

        public static object Construct(Type type, FrameworkElement element = null, List<object> args = null)
        {
            args = args ?? new List<object>();

            return Manager.SurroundConstruct(type, element, args, Manager.ConstructCore);
        }


        public static T SurroundConstruct<T>(this FrameworkElement element, List<object> args,  Func<FrameworkElement, List<object>, T> constructor)
            where T : ModifiableEntity
        {
            return (T)SurroundConstruct(typeof(T), element, args, (_type, _element, _args) => constructor(_element, _args));
        }

        public static T SurroundConstruct<T>(this FrameworkElement element, Func<T> constructor)
            where T : ModifiableEntity
        {
            return (T)SurroundConstruct(typeof(T), element, null, (_type, _element, _args) => constructor());
        }

        public static object SurroundConstruct(Type type, FrameworkElement element, List<object> args, Func<Type, FrameworkElement, List<object>, object> constructor)
        {
            return Manager.SurroundConstruct(type, element, args, constructor);
        }
       

        public static void Register<T>(Func<FrameworkElement, List<object>, T> constructor)
            where T : ModifiableEntity
        {
            Manager.Constructors.Add(typeof(T), constructor);
        }

        
    }

    public class ConstructorManager
    {
        public event Func<Type, FrameworkElement, List<object>, bool> PreConstructors;

        public Func<Type, Func<FrameworkElement, List<object>, object>> GlobalConstructor;
        public Dictionary<Type, Func<FrameworkElement, List<object>, object>> Constructors = new Dictionary<Type, Func<FrameworkElement, List<object>, object>>();

        public event Func<Type, FrameworkElement, List<object>, object, bool> PostConstructors;

        public ConstructorManager()
        {
            PostConstructors += PostConstructors_AddFilterProperties;
        }

        public virtual object ConstructCore(Type type, FrameworkElement element = null, List<object> args = null)
        {
            args = args ?? new List<object>();

            Func<FrameworkElement, List<object>, object> c = Constructors.TryGetC(type);
            if (c != null)
            {
                object result = c(element, args);
                return result;
            }

            if (GlobalConstructor != null)
            {
                foreach (Func<Type, Func<FrameworkElement, List<object>, object>> factory in GlobalConstructor.GetInvocationList())
                {
                    var func = factory(type);

                    if (func != null)
                        return func(element, args);
                }
            }

            return Activator.CreateInstance(type);
        }

        public virtual  object SurroundConstruct(Type type, FrameworkElement element, List<object> args, Func<Type, FrameworkElement, List<object>, object> constructor)
        {
            args = args ?? new List<object>();

            if (PreConstructors != null)
                foreach (Func<Type, FrameworkElement, List<object>, bool> pre in PreConstructors.GetInvocationList())
                    if (!pre(type, element, args))
                        return null;

            var entity = constructor(type, element, args);

            if (entity == null)
                return null;

            if (PostConstructors != null)
                foreach (Func<Type, FrameworkElement, List<object>, object, bool> post in PostConstructors.GetInvocationList())
                    if (!post(type, element, args, entity))
                        return null;

            return entity;
        }


        public static bool PostConstructors_AddFilterProperties(Type type, FrameworkElement element, List<object> args, object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("result");

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

            return true;
        }

    }
}
