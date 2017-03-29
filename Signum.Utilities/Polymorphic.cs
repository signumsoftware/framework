using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.Diagnostics;

namespace Signum.Utilities
{
    public static class PolymorphicMerger
    {
        public static T Inheritance<T>(KeyValuePair<Type, T> currentValue, KeyValuePair<Type, T> baseValue, List<KeyValuePair<Type, T>> newInterfacesValues) where T : class
        {
            return currentValue.Value ?? baseValue.Value;
        }

        public static T InheritanceAndInterfaces<T>(KeyValuePair<Type, T> currentValue, KeyValuePair<Type, T> baseValue, List<KeyValuePair<Type, T>> newInterfacesValues) where T : class
        {
            var result = currentValue.Value ?? baseValue.Value;

            if (result != null)
                return result;

            var conflicts = newInterfacesValues.Where(a => a.Value != null);

            if (conflicts.Count() > 1)
                throw new InvalidOperationException("Ambiguity for type {0} between interfaces {1}".FormatWith(currentValue.Key.Name, newInterfacesValues.CommaAnd(t => t.Key.Name)));

            return conflicts.Select(a => a.Value).SingleOrDefaultEx();
        }

        public static Dictionary<K, V> InheritDictionary<K, V>(KeyValuePair<Type, Dictionary<K, V>> currentValue, KeyValuePair<Type, Dictionary<K, V>> baseValue, List<KeyValuePair<Type, Dictionary<K, V>>> newInterfacesValues)
        {
            if (currentValue.Value == null && baseValue.Value == null)
                return null;

            Dictionary<K, V> newDictionary = new Dictionary<K, V>();

            if (baseValue.Value != null)
                newDictionary.AddRange(baseValue.Value);

            if (currentValue.Value != null)
                newDictionary.SetRange(currentValue.Value);

            return newDictionary;
        }

        public static Dictionary<K, V> InheritDictionaryInterfaces<K, V>(KeyValuePair<Type, Dictionary<K, V>> currentValue, KeyValuePair<Type, Dictionary<K, V>> baseValue, List<KeyValuePair<Type, Dictionary<K, V>>> newInterfacesValues)
        {
            if (currentValue.Value == null && baseValue.Value == null && newInterfacesValues.All(a => a.Value == null))
                return null;

            Dictionary<K, V> newDictionary = new Dictionary<K, V>();

            if (baseValue.Value != null)
                newDictionary.AddRange(baseValue.Value);

            if (currentValue.Value != null)
                newDictionary.SetRange(currentValue.Value);

            var interfaces = newInterfacesValues.Where(a => a.Value != null);

            var keys = interfaces.SelectMany(inter => inter.Value.Keys).Distinct().Except(newDictionary.Keys);

            foreach (var item in keys)
            {
                var types = interfaces.Where(a => a.Value.ContainsKey(item)).Select(a => new { a.Key, Value = a.Value[item] }).ToList();

                var groups = types.GroupBy(t => t.Value);
                if (groups.Count() > 1)
                    throw new InvalidOperationException("Ambiguity for key {0} in type {0} between interfaces {1}".FormatWith(item, currentValue.Key.Name, types.CommaAnd(t => t.Key.Name)));

                newDictionary[item] = groups.Single().Key;
            }

            return newDictionary;
        }
    }

    public delegate T PolymorphicMerger<T>(KeyValuePair<Type, T> currentValue, KeyValuePair<Type, T> baseValue, List<KeyValuePair<Type, T>> newInterfacesValues) where T : class;

    public class Polymorphic<T> where T : class
    {
        Dictionary<Type, T> definitions = new Dictionary<Type, T>();
        ConcurrentDictionary<Type, T> cached = new ConcurrentDictionary<Type, T>();

        PolymorphicMerger<T> merger;
        Type minimumType;


       public bool ContainsKey(Type type)
        {
           return definitions.ContainsKey(type);

        }

        bool IsAllowed(Type type)
        {
            return minimumType == null || minimumType.IsAssignableFrom(type);
        }

        void AssertAllowed(Type type)
        {
            if (!IsAllowed(type))
                throw new InvalidOperationException("{0} is not a {1}".FormatWith(type.Name, minimumType.Name));
        }

        public Polymorphic(PolymorphicMerger<T> merger = null, Type minimumType = null)
        {
            this.merger = merger ?? PolymorphicMerger.Inheritance<T>;
            this.minimumType = minimumType ?? GetDefaultType(typeof(T));
        }

        private static Type GetDefaultType(Type type)
        {
            if (!typeof(Delegate).IsAssignableFrom(type))
                return null;

            MethodInfo mi = type.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance);

            var param = mi.GetParameters().FirstOrDefault();

            if (param == null)
                return null;

            return param.ParameterType;
        }

        public T GetValue(Type type)
        {
            var result = TryGetValue(type);

            if (result == null)
                throw new InvalidOperationException("No value defined for type {0}".FormatWith(type));

            return result;
        }

        public T TryGetValue(Type type)
        {
            AssertAllowed(type);

            return cached.GetOrAdd(type, TryGetValueInternal);
        }

        public T GetDefinition(Type type)
        {
            AssertAllowed(type);

            return definitions.TryGetC(type);
        }

        T TryGetValueInternal(Type type)
        {
            if (cached.TryGetValue(type, out T result))
                return result;

            var baseValue = type.BaseType == null || !IsAllowed(type.BaseType) ? null : TryGetValue(type.BaseType);

            var currentValue = definitions.TryGetC(type);

            if (minimumType != null && !minimumType.IsInterface)
                return merger(KVP.Create(type, currentValue), KVP.Create(type.BaseType, baseValue), null);

            IEnumerable<Type> interfaces = type.GetInterfaces().Where(IsAllowed);

            if (type.BaseType != null)
                interfaces = interfaces.Except(type.BaseType.GetInterfaces());

            return merger(KVP.Create(type, currentValue), KVP.Create(type.BaseType, baseValue), interfaces.Select(inter => KVP.Create(inter, TryGetValue(inter))).ToList());
        }

        public void SetDefinition(Type type, T value)
        {
            AssertAllowed(type);

            definitions[type] = value;

            ClearCache();
        }

        public void ClearCache()
        {
            cached.Clear();
        }

        public IEnumerable<Type> OverridenTypes
        {
            get { return definitions.Keys; }
        }

        public IEnumerable<T> OverridenValues
        {
            get { return definitions.Values; }
        }

        public Dictionary<Type, T> ExportDefinitions()
        {
            return this.definitions;
        }

        public void ImportDefinitions(Dictionary<Type, T> dic)
        {
            this.definitions = dic;
            this.ClearCache();
        }
    }

    [DebuggerStepThrough]
    public static class PolymorphicExtensions
    {
        public static T GetOrAddDefinition<T>(this Polymorphic<T> polymorphic, Type type) where T : class, new()
        {
            T value = polymorphic.GetDefinition(type);

            if (value != null)
                return value;

            value = new T();

            polymorphic.SetDefinition(type, value);

            return value;
        }


        public static void Register<T, S>(this Polymorphic<Action<T>> polymorphic, Action<S> action) where S : T
        {
            polymorphic.SetDefinition(typeof(S), t => action((S)t));
        }

        public static void Register<T, S, P0>(this Polymorphic<Action<T, P0>> polymorphic, Action<S, P0> action) where S : T
        {
            polymorphic.SetDefinition(typeof(S), (t, p0) => action((S)t, p0));
        }

        public static void Register<T, S, P0, P1>(this Polymorphic<Action<T, P0, P1>> polymorphic, Action<S, P0, P1> action) where S : T
        {
            polymorphic.SetDefinition(typeof(S), (t, p0, p1) => action((S)t, p0, p1));
        }

        public static void Register<T, S, P0, P1, P2>(this Polymorphic<Action<T, P0, P1, P2>> polymorphic, Action<S, P0, P1, P2> action) where S : T
        {
            polymorphic.SetDefinition(typeof(S), (t, p0, p1, p2) => action((S)t, p0, p1, p2));
        }

        public static void Register<T, S, P0, P1, P2, P3>(this Polymorphic<Action<T, P0, P1, P2, P3>> polymorphic, Action<S, P0, P1, P2, P3> action) where S : T
        {
            polymorphic.SetDefinition(typeof(S), (t, p0, p1, p2, p3) => action((S)t, p0, p1, p2, p3));
        }


        public static void Register<T, S, R>(this Polymorphic<Func<T, R>> polymorphic, Func<S, R> func) where S : T
        {
            polymorphic.SetDefinition(typeof(S), t => func((S)t));
        }

        public static void Register<T, S, P0, R>(this Polymorphic<Func<T, P0, R>> polymorphic, Func<S, P0, R> func) where S : T
        {
            polymorphic.SetDefinition(typeof(S), (t, p0) => func((S)t, p0));
        }

        public static void Register<T, S, P0, P1, R>(this Polymorphic<Func<T, P0, P1, R>> polymorphic, Func<S, P0, P1, R> func) where S : T
        {
            polymorphic.SetDefinition(typeof(S), (t, p0, p1) => func((S)t, p0, p1));
        }

        public static void Register<T, S, P0, P1, P2, R>(this Polymorphic<Func<T, P0, P1, P2, R>> polymorphic, Func<S, P0, P1, P2, R> func) where S : T
        {
            polymorphic.SetDefinition(typeof(S), (t, p0, p1, p2) => func((S)t, p0, p1, p2));
        }

        public static void Register<T, S, P0, P1, P2, P3, R>(this Polymorphic<Func<T, P0, P1, P2, P3, R>> polymorphic, Func<S, P0, P1, P2, P3, R> func) where S : T
        {
            polymorphic.SetDefinition(typeof(S), (t, p0, p1, p2, p3) => func((S)t, p0, p1, p2, p3));
        }


        public static void Invoke<T>(this Polymorphic<Action<T>> polymorphic, T instance)
        {
            var action = polymorphic.GetValue(instance.GetType());
            action(instance);
        }

        public static void Invoke<T, P0>(this Polymorphic<Action<T, P0>> polymorphic, T instance, P0 p0)
        {
            var action = polymorphic.GetValue(instance.GetType());
            action(instance, p0);
        }

        public static void Invoke<T, P0, P1>(this Polymorphic<Action<T, P0, P1>> polymorphic, T instance, P0 p0, P1 p1)
        {
            var action = polymorphic.GetValue(instance.GetType());
            action(instance, p0, p1);
        }

        public static void Invoke<T, P0, P1, P2>(this Polymorphic<Action<T, P0, P1, P2>> polymorphic, T instance, P0 p0, P1 p1, P2 p2)
        {
            var action = polymorphic.GetValue(instance.GetType());
            action(instance, p0, p1, p2);
        }

        public static void Invoke<T, P0, P1, P2, P3>(this Polymorphic<Action<T, P0, P1, P2, P3>> polymorphic, T instance, P0 p0, P1 p1, P2 p2, P3 p3)
        {
            var action = polymorphic.GetValue(instance.GetType());
            action(instance, p0, p1, p2, p3);
        }



        public static R Invoke<T, R>(this Polymorphic<Func<T, R>> polymorphic, T instance)
        {
            var func = polymorphic.GetValue(instance.GetType());
            return func(instance);
        }

        public static R Invoke<T, P0, R>(this Polymorphic<Func<T, P0, R>> polymorphic, T instance, P0 p0)
        {
            var func = polymorphic.GetValue(instance.GetType());
            return func(instance, p0);
        }

        public static R Invoke<T, P0, P1, R>(this Polymorphic<Func<T, P0, P1, R>> polymorphic, T instance, P0 p0, P1 p1)
        {
            var func = polymorphic.GetValue(instance.GetType());
            return func(instance, p0, p1);
        }

        public static R Invoke<T, P0, P1, P2, R>(this Polymorphic<Func<T, P0, P1, P2, R>> polymorphic, T instance, P0 p0, P1 p1, P2 p2)
        {
            var func = polymorphic.GetValue(instance.GetType());
            return func(instance, p0, p1, p2);
        }

        public static R Invoke<T, P0, P1, P2, P3, R>(this Polymorphic<Func<T, P0, P1, P2, P3, R>> polymorphic, T instance, P0 p0, P1 p1, P2 p2, P3 p3)
        {
            var func = polymorphic.GetValue(instance.GetType());
            return func(instance, p0, p1, p2, p3);
        }
    }
}
