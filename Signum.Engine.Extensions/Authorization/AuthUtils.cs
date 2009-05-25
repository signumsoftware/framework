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
        public static Dictionary<K, V> OuterCollapseDictionariesC<K, V>(this IEnumerable<Dictionary<K, V>> dictionaries, Func<IEnumerable<V>, V> mixer)
            where V: class
        {
            var dicList = dictionaries.ToList();

            var keys = dicList.NotNull().SelectMany(d => d.Keys).ToHashSet();

            return keys.ToDictionary(k => k, k => mixer(dicList.Select(d => d.TryGetC(k))));
        }

        public static Dictionary<K, V> OuterCollapseDictionariesS<K, V>(this IEnumerable<Dictionary<K, V>> dictionaries, Func<IEnumerable<V?>, V> mixer)
            where V : struct
        {
            var dicList = dictionaries.ToList();

            var keys = dicList.NotNull().SelectMany(d => d.Keys).ToHashSet();

            return keys.ToDictionary(k => k, k => mixer(dicList.Select(d => d.TryGetS(k))));
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

        public static Dictionary<K, V> OverrideJoin<K, V>(this Dictionary<K, V> dictionary, Dictionary<K, V> newValues, Func<K,V,V,V> func)
            where V: class
        {
            if (newValues == null)
                return dictionary;

            if (dictionary == null)
                return newValues;

            return dictionary.OuterJoinDictionaryCC(newValues, func); ;
        }


        public static Dictionary<K, V> Simplify<K,V>(this Dictionary<K, V> dictionary, Func<V,bool> func)
        {
            if (dictionary == null || dictionary.Count == 0)
                return null;

            dictionary.RemoveRange(dictionary.Where(p => func(p.Value)).Select(p => p.Key).ToList());

            if (dictionary.Count == 0)
                return null;

            return dictionary;
        }

   
        public static Access MaxAccess(this IEnumerable<Access> collection)
        {
            Access max = Access.None;
            foreach (var item in collection)
            {
                if (item == Access.Modify)
                    return Access.Modify;

                if (item > Access.None)
                    max = item;
            }

            return max;
        }

        public static Access MaxAccess(this IEnumerable<Access?> collection)
        {
            Access max = Access.None;
            foreach (var item in collection)
            {
                if (item == null || item.Value == Access.Modify)
                    return Access.Modify;

                if (item.Value > Access.None)
                    max = item.Value;
            }

            return max;
        }

        public static TypeAccess MaxTypeAccess(this IEnumerable<TypeAccess> collection)
        {
            TypeAccess max = TypeAccess.None;
            foreach (var item in collection)
            {
                if (item == TypeAccess.Create)
                    return TypeAccess.Create;

                if (item > TypeAccess.None)
                    max = item;
            }

            return max;
        }

        public static TypeAccess MaxTypeAccess(this IEnumerable<TypeAccess?> collection)
        {
            TypeAccess max = TypeAccess.None;
            foreach (var item in collection)
            {
                if (item == null || item.Value == TypeAccess.Create)
                    return TypeAccess.Create;

                if (item.Value > TypeAccess.None)
                    max = item.Value;
            }

            return max;
        }

        public static bool MaxAllowed(this IEnumerable<bool> collection)
        {
            foreach (var item in collection)
            {
                if (item)
                    return true;
            }

            return false;
        }

        public static bool MaxAllowed(this IEnumerable<bool?> collection)
        {
            foreach (var item in collection)
            {
                if (item == null || item.Value)
                    return true;
            }

            return false;
        }

    }
}
