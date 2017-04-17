using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.DataStructures;

namespace Signum.Utilities
{
    public static class ListExtensions
    {
        public static List<T> Extract<T>(this IList<T> list, Func<T, bool> condition)
        {
            List<T> result = new List<T>();
            int i = 0;
            while (i < list.Count)
            {
                if (condition(list[i]))
                {
                    result.Add(list[i]);
                    list.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
            return result;
        }


        public static void Sort<T, A>(this List<T> list, Func<T, A> element)
            where A : IComparable<A>
        {
            list.Sort((a, b) => element(a).CompareTo(element(b)));
        }

        public static void SortDescending<T, A>(this List<T> list, Func<T, A> element)
           where A : IComparable<A>
        {
            list.Sort((a, b) => element(b).CompareTo(element(a)));
        }

        public static void RemoveAll<T>(this IList<T> list, Func<T, bool> condition)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (condition(list[i]))
                    list.RemoveAt(i);
            }
        }

        public static void AddRange<T>(this IList<T> list, IEnumerable<T> elements)
        {
            foreach (var item in elements)
            {
                list.Add(item); 
            }
        }
        
        public static void AddRange<T>(this IList<T> list, params T[] elements)
        {
            foreach (var item in elements)
            {
                list.Add(item); 
            }
        }
    }
}
