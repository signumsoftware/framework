using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.Reflection;

namespace Signum.Engine
{
    public class ExternalPolymorphicDictionary<K, V> where V : class
    {
        Dictionary<Type, Dictionary<K, V>> dictionary = new Dictionary<Type, Dictionary<K, V>>();

        public Dictionary<Type, Dictionary<K, V>> RawDictionary { get { return dictionary; } }

        public V TryGetValue(Type type, K key)
        {
            if (!type.IsIIdentifiable())
                throw new InvalidOperationException("Type {0} has to implement at least {1}".Formato(type, typeof(IIdentifiable)));

            V result = type.FollowC(t => t.BaseType)
                .TakeWhile(t => typeof(IdentifiableEntity).IsAssignableFrom(t))
                .Select(t => dictionary.TryGetC(t).TryGetC(key)).NotNull().FirstOrDefault();

            if (result != null)
                return result;

            List<Type> interfaces = type.GetInterfaces()
                .Where(t => t.IsIIdentifiable() && dictionary.TryGetC(t).TryGetC(key) != null)
                .ToList();

            if (interfaces.Count > 1)
                throw new InvalidOperationException("Ambiguity between interfaces: {0}".Formato(interfaces.ToString(", ")));

            if (interfaces.Count < 1)
                return null;

            return dictionary[interfaces.Single()][key];
        }

        public HashSet<K> GetAllKeys(Type type)
        {
            if (!type.IsIIdentifiable())
                throw new InvalidOperationException("Type {0} has to implement at least {1}".Formato(type, typeof(IIdentifiable)));
            
            HashSet<K> result = type.FollowC(t => t.BaseType)
                    .TakeWhile(t => typeof(IdentifiableEntity).IsAssignableFrom(t))
                    .Select(t => dictionary.TryGetC(t)).NotNull().SelectMany(d => d.Keys).ToHashSet();

            result.UnionWith(type.GetInterfaces()
                .Where(t => t.IsIIdentifiable())
                .Select(t => dictionary.TryGetC(t))
                .NotNull().SelectMany(d => d.Keys));

            return result;
        }

        public HashSet<K> GetAllKeys()
        {
            return dictionary.Values.SelectMany(a => a.Keys).ToHashSet();
        }

        public void Add(Type type, K key, V value)
        {
            dictionary.GetOrCreate(type)[key] = value;
        }

        public Type[] ImplementingTypes(K key)
        {
            return dictionary.Where(o => o.Value.ContainsKey(key)).Select(a => a.Key).ToArray();
        }
    }
}
