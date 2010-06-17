using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Entities.Authorization;

namespace Signum.Engine.Authorization
{
    static class AuthUtils
    {
        public static Dictionary<K, V> OuterCollapseDictionariesS<K, V>(this IEnumerable<Dictionary<K, V>> dictionaries, V defaultValue, Func<IEnumerable<V>, V> mixer)
        {
            var dicList = dictionaries.ToList();

            var keys = dicList.NotNull().SelectMany(d => d.Keys).ToHashSet();

            return keys.ToDictionary(k => k, k => mixer(dicList.Select(d => d.TryGet(k, defaultValue))));
        }

        public static Dictionary<K, V> Override<K, V>(this Dictionary<K, V> dictionary, Dictionary<K, V> newValues)
        {
            if (newValues == null)
                return dictionary;

            if (dictionary == null)
                return newValues;

            dictionary.SetRange(newValues);

            return dictionary; 
        }


        public static Dictionary<K, V> Simplify<K, V>(this Dictionary<K, V> dictionary, Func<V, bool> func)
        {
            if (dictionary == null || dictionary.Count == 0)
                return null;

            dictionary.RemoveRange(dictionary.Where(p => func(p.Value)).Select(p => p.Key).ToList());

            if (dictionary.Count == 0)
                return null;

            return dictionary;
        }

        public static bool MaxAllowed(this IEnumerable<bool> collection)
        {
            return collection.Any(a => a);
        }

    }
}
