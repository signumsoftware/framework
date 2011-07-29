using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

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
                throw new InvalidOperationException("Ambiguity for type {0} between interfaces {1}".Formato(currentValue.Key.Name, newInterfacesValues.CommaAnd(t => t.Key.Name)));

            return conflicts.Select(a => a.Value).SingleOrDefault(); 
        }

         public static Dictionary<K, V> InheritDictionary<K, V>(KeyValuePair<Type, Dictionary<K, V>> currentValue, KeyValuePair<Type, Dictionary<K, V>> baseValue, List<KeyValuePair<Type, Dictionary<K, V>>> newInterfacesValues)
        {
            if (currentValue.Value == null || baseValue.Value == null || newInterfacesValues.All(a => a.Value == null))
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

                if (types.Count > 1)
                    throw new InvalidOperationException("Ambiguity for key {0} in type {0} between interfaces {1}".Formato(item, currentValue.Key.Name, types.CommaAnd(t => t.Key.Name)));

                newDictionary[item] = types.Single().Value;
            }

            return newDictionary;
        }

        public static T GetOrAdd<T>(this Polymorphic<T> polymorophic, Type type) where T: class, new()
        {
            T value = polymorophic.GetDefinition(type);

            if (value != null)
                return value;

            value = new T();

            polymorophic.Add(type, value);

            return value;
        }
    }

    public delegate T PolymorphicMerger<T>(KeyValuePair<Type, T> currentValue, KeyValuePair<Type, T> baseValue, List<KeyValuePair<Type, T>> newInterfacesValues) where T : class;
 
    public class Polymorphic<T> where T : class
    {
        Dictionary<Type, T> definitions = new Dictionary<Type, T>();
        ConcurrentDictionary<Type, T> cached = new ConcurrentDictionary<Type, T>();

        PolymorphicMerger<T> merger;
        Type minimumType;

        bool IsAllowed(Type type)
        {
            return minimumType == null || minimumType.IsAssignableFrom(type);
        }

        void AssertAllowed(Type type)
        {
            if (!IsAllowed(type))
                throw new InvalidOperationException("{0} is not a {1}".Formato(type.Name, minimumType.Name));
        }

        public Polymorphic(PolymorphicMerger<T> merger, Type type)
        {
            this.merger = merger;
            this.minimumType = type;
        }

        public Polymorphic() : this(PolymorphicMerger.InheritanceAndInterfaces<T>, null)
        {
        }

        public T GetValue(Type type)
        {
            var result = TryGetValue(type);
         
            if (result == null)
                throw new InvalidOperationException("No value defined for type {0}".Formato(type));

            return result; 
        }

        public T TryGetValue(Type type)
        {
            IsAllowed(type);

            return cached.GetOrAdd(type, TryGetValueInternal);
        }

        public T GetDefinition(Type type)
        {
            AssertAllowed(type);

            return definitions.TryGetC(type);
        }

        T TryGetValueInternal(Type type)
        {
            T result;
            if (cached.TryGetValue(type, out result))
                return result;

            var baseValue = type.BaseType == null  || !IsAllowed(type.BaseType) ? null : TryGetValue(type.BaseType);

            var currentValue = definitions.TryGetC(type); 

            IEnumerable<Type> interfaces = type.GetInterfaces().Where(IsAllowed);

            if(type.BaseType != null)
                interfaces = interfaces.Except(type.BaseType.GetInterfaces());

            return merger(KVP.Create(type, currentValue), KVP.Create(type.BaseType, baseValue), interfaces.Select(inter => KVP.Create(inter, TryGetValue(inter))).ToList()); 
        }

        public void Add(Type type, T value)
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
    }
}
