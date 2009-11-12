using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Globalization;
using Signum.Utilities.Synchronization;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Data;
using System.Text.RegularExpressions;
using Signum.Utilities.Properties;

namespace Signum.Utilities
{
    public static class EnumerableExtensions
    {
        public static bool Empty<T>(this IEnumerable<T> collection)
        {
            foreach (var item in collection)
                return false;

            return true;
        }

        public static IEnumerable<T> NotNull<T>(this IEnumerable<T> collection) where T : class
        {
            return collection.Where(a => a != null);
        }

        public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> collection) where T : struct
        {
            return collection.Where(a => a.HasValue).Select(a => a.Value);
        }

        public static IEnumerable<T> And<T>(this IEnumerable<T> collection, T newItem)
        {
            foreach (var item in collection)
                yield return item;
            yield return newItem;
        }

        public static IEnumerable<T> PreAnd<T>(this IEnumerable<T> collection, T newItem)
        {
            yield return newItem;
            foreach (var item in collection)
                yield return item;
        }

        public static int IndexOf<T>(this IEnumerable<T> collection, T item)
        {
            int i = 0;
            foreach (var val in collection)
            {
                if (EqualityComparer<T>.Default.Equals(item, val))
                    return i;
                i++;
            }
            return -1;
        }

        public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> condition)
        {

            int i = 0;
            foreach (var val in collection)
            {
                if (condition(val))
                    return i;
                i++;
            }
            return -1;
        }

        public static T Single<T>(this IEnumerable<T> collection, string errorMessage)
        {
            return collection.Single<T>(errorMessage, errorMessage);
        }

        public static T Single<T>(this IEnumerable<T> collection, string errorZero, string errorMoreThanOne)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException(errorZero);

                T current = enumerator.Current;

                if (!enumerator.MoveNext())
                    return current;
            }

            throw new InvalidOperationException(errorMoreThanOne);
        }

        public static T SingleOrDefault<T>(this IEnumerable<T> collection, string errorMessage)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return default(T);

                T current = enumerator.Current;

                if (!enumerator.MoveNext())
                    return current;
            }

            throw new InvalidOperationException(errorMessage);
        }

        public static T First<T>(this IEnumerable<T> collection, string errorMessage)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException(errorMessage);

                return enumerator.Current;
            }
        }

        //public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        //{
        //    foreach (var item in collection)
        //    {
        //        action(item);
        //        yield return item; 
        //    }
        //}

        //public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T, int> action)
        //{
        //    int i = 0;
        //    foreach (var item in collection)
        //    {
        //        action(item, i);
        //        yield return item;
        //        i++;
        //    }
        //}

        public static string ToString<T>(this IEnumerable<T> collection, string separator)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in collection)
            {
                sb.Append(item.ToString());
                sb.Append(separator);
            }
            return sb.ToString(0, Math.Max(0, sb.Length - separator.Length));  // Remove at the end is faster
        }

        public static string ToString<T>(this IEnumerable<T> collection, Func<T, string> toString, string separator)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in collection)
            {
                sb.Append(toString(item));
                sb.Append(separator);
            }
            return sb.ToString(0, Math.Max(0, sb.Length - separator.Length));  // Remove at the end is faster
        }

        public static void ToConsole<T>(this IEnumerable<T> collection)
        {
            ToConsole(collection, a => a.ToString());
        }

        public static void ToConsole<T>(this IEnumerable<T> collection, Func<T, string> toString)
        {
            foreach (var item in collection)
                Console.WriteLine(toString(item));
        }

        public static void ToFile(this IEnumerable<string> collection, string fileName)
        {
            using (FileStream fs = File.Create(fileName))
            using (StreamWriter sw = new StreamWriter(fs, Encoding.Default))
            {
                foreach (var item in collection)
                    sw.WriteLine(item);
            }
        }

        public static void ToFile<T>(this IEnumerable<T> collection, Func<T, string> toString, string fileName)
        {
            collection.Select(toString).ToFile(fileName);
        }

        public static DataTable ToDataTable<T>(this IEnumerable<T> collection)
        {
            DataTable table = new DataTable();

            List<MemberEntry<T>> members = MemberEntryFactory.GenerateList<T>();
            table.Columns.AddRange(members.Select(m => new DataColumn(m.Name, m.MemberInfo.ReturningType())).ToArray());
            foreach (var e in collection)
                table.Rows.Add(members.Select(m => m.Getter(e)).ToArray());
            return table;
        }

        #region String Tables
        public static string[,] ToStringTable<T>(this IEnumerable<T> collection)
        {
            List<MemberEntry<T>> members = MemberEntryFactory.GenerateList<T>();

            string[,] result = new string[members.Count, collection.Count() + 1];

            for (int i = 0; i < members.Count; i++)
                result[i, 0] = members[i].Name;

            int j = 1;
            foreach (var item in collection)
            {
                for (int i = 0; i < members.Count; i++)
                    result[i, j] = members[i].Getter(item).TryCC(a => a.ToString()) ?? "";
                j++;
            }

            return result;
        }

        public static string FormatTable(this string[,] table)
        {
            return FormatTable(table, true);
        }

        public static string FormatTable(this string[,] table, bool longHeaders)
        {
            int width = table.GetLength(0);
            int height = table.GetLength(1);
            int start = height == 1 ? 0 : (longHeaders ? 0 : 1);

            int[] lengths = 0.To(width).Select(i => Math.Max(3, start.To(height).Max(j => table[i, j].Length))).ToArray();

            return 0.To(height).Select(j => 0.To(width).ToString(i => table[i, j].PadChopRight(lengths[i]), " ")).ToString("\r\n");
        }

        public static void WriteFormatedStringTable<T>(this IEnumerable<T> collection, TextWriter textWriter, string title)
        {
            textWriter.WriteLine();
            textWriter.WriteLine(title ?? "Tabla");
            textWriter.WriteLine(collection.ToStringTable().FormatTable(false));
            textWriter.WriteLine();
        }

        public static void ToConsoleTable<T>(this IEnumerable<T> collection, string title)
        {
            collection.WriteFormatedStringTable(Console.Out, title);
        }

        public static string ToWikiTable<T>(this IEnumerable<T> collection)
        {
            string[,] table = collection.ToStringTable();

            string str = "{| class=\"data\"\r\n" + 0.To(table.GetLength(1))
                .Select(i => (i == 0 ? "! " : "| ") + table.Row(i).ToString(o => o == null ? "" : o.ToString(), i == 0 ? " !! " : " || "))
                .ToString("\r\n|-\r\n") + "\r\n|}";

            return str;
        }
        #endregion

        #region Min Max
        public static T WithMin<T, V>(this IEnumerable<T> collection, Func<T, V> valueSelector)
          where V : IComparable<V>
        {
            T result = default(T);
            bool hasMin = false;
            V min = default(V);
            foreach (var item in collection)
            {
                V val = valueSelector(item);
                if (!hasMin || val.CompareTo(min) < 0)
                {
                    hasMin = true;
                    min = val;
                    result = item;
                }
            }

            return result;
        }

        public static T WithMax<T, V>(this IEnumerable<T> collection, Func<T, V> valueSelector)
               where V : IComparable<V>
        {
            T result = default(T);
            bool hasMax = false;
            V max = default(V);

            foreach (var item in collection)
            {
                V val = valueSelector(item);
                if (!hasMax || val.CompareTo(max) > 0)
                {
                    hasMax = true;
                    max = val;
                    result = item;
                }
            }
            return result;
        }

        public static MinMax<T> WithMinMaxPair<T, V>(this IEnumerable<T> collection, Func<T, V> valueSelector)
        where V : IComparable<V>
        {
            T withMin = default(T), withMax = default(T);
            bool hasMin = false, hasMax = false;
            V min = default(V), max = default(V);
            foreach (var item in collection)
            {
                V val = valueSelector(item);
                if (!hasMax || val.CompareTo(max) > 0)
                {
                    hasMax = true;
                    max = val;
                    withMax = item;
                }

                if (!hasMin || val.CompareTo(min) < 0)
                {
                    hasMin = true;
                    min = val;
                    withMin = item;
                }
            }

            return new MinMax<T>(withMin, withMax);
        }

        public static MinMax<T> MinMaxPair<T>(this IEnumerable<T> collection)
    where T : IComparable<T>
        {
            bool has = false;
            T min = default(T), max = default(T);
            foreach (var item in collection)
            {
                if (!has)
                {
                    has = true;
                    min = max = item;
                }
                else
                {
                    if (item.CompareTo(max) > 0)
                        max = item;
                    if (item.CompareTo(min) < 0)
                        min = item;
                }
            }

            return new MinMax<T>(min, max);
        }

        public static MinMax<V> MinMaxPair<T, V>(this IEnumerable<T> collection, Func<T, V> valueSelector)
            where V : IComparable<V>
        {
            bool has = false;
            V min = default(V), max = default(V);
            foreach (var item in collection)
            {
                V val = valueSelector(item);

                if (!has)
                {
                    has = true;
                    min = max = val;
                }
                else
                {
                    if (val.CompareTo(max) > 0)
                        max = val;
                    if (val.CompareTo(min) < 0)
                        min = val;
                }
            }

            return new MinMax<V>(min, max);
        }


        #endregion

        #region Operation
        public static IEnumerable<S> BiSelect<T, S>(this IEnumerable<T> collection, Func<T, T, S> func)
        {
            return BiSelect(collection, func, BiSelectOptions.None);
        }

        public static IEnumerable<S> BiSelect<T, S>(this IEnumerable<T> collection, Func<T, T, S> func, BiSelectOptions options)
        {
            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    yield break;


                T firstItem = enumerator.Current;
                if (options == BiSelectOptions.Initial || options == BiSelectOptions.InitialAndFinal)
                    yield return func(default(T), firstItem);

                T lastItem = firstItem;
                while (enumerator.MoveNext())
                {
                    T item = enumerator.Current;
                    yield return func(lastItem, item);
                    lastItem = item;
                }

                if (options == BiSelectOptions.Final || options == BiSelectOptions.InitialAndFinal)
                    yield return func(lastItem, default(T));

                if (options == BiSelectOptions.Circular)
                    yield return func(lastItem, firstItem);
            }
        }

        //return one element more
        public static IEnumerable<S> SelectAggregate<T, S>(this IEnumerable<T> collection, S seed, Func<S, T, S> aggregate)
        {
            yield return seed;
            foreach (var item in collection)
            {
                seed = aggregate(seed, item);
                yield return seed;
            }
        }

        public static List<IGrouping<T, T>> GroupWhen<T>(this IEnumerable<T> collection, Func<T, bool> isGroupKey)
        {
            List<IGrouping<T, T>> result = new List<IGrouping<T, T>>();
            Grouping<T, T> group = null;
            foreach (var item in collection)
            {
                if (isGroupKey(item))
                {
                    group = new Grouping<T, T>(item);
                    result.Add(group);
                }
                else
                {
                    if (group != null)
                        group.Add(item);
                }
            }

            return result;
        }

        public static IEnumerable<List<T>> GroupsOf<T>(this IEnumerable<T> collection, int groupSize)
        {
            List<T> newList = new List<T>(groupSize);
            foreach (var item in collection)
            {
                newList.Add(item);
                if (newList.Count == groupSize)
                {
                    yield return newList;
                    newList = new List<T>();
                }
            }

            if (newList.Count != 0)
                yield return newList;
        }

        public static IEnumerable<T> Slice<T>(this IEnumerable<T> collection, int firstIncluded, int toNotIncluded)
        {
            return collection.Skip(firstIncluded).Take(toNotIncluded - firstIncluded);
        }

        public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> collection) where T : IComparable<T>
        {
            return collection.OrderBy(a => a);
        }

        public static IOrderedEnumerable<T> OrderDescending<T>(this IEnumerable<T> collection) where T : IComparable<T>
        {
            return collection.OrderByDescending(a => a);
        }
        #endregion

        #region Zip
        public static IEnumerable<Tuple<A, B>> Zip<A, B>(this IEnumerable<A> colA, IEnumerable<B> colB)
        {
            using (var enumA = colA.GetEnumerator())
            using (var enumB = colB.GetEnumerator())
            {
                while (enumA.MoveNext() && enumB.MoveNext())
                {
                    yield return new Tuple<A, B>(enumA.Current, enumB.Current);
                }
            }
        }

        public static IEnumerable<R> Zip<A, B, R>(this IEnumerable<A> colA, IEnumerable<B> colB, Func<A, B, R> mixer)
        {
            using (var enumA = colA.GetEnumerator())
            using (var enumB = colB.GetEnumerator())
            {
                while (enumA.MoveNext() && enumB.MoveNext())
                {
                    yield return mixer(enumA.Current, enumB.Current);
                }
            }
        }

        public static void ZipForeach<A, B>(this IEnumerable<A> colA, IEnumerable<B> colB, Action<A, B> actions)
        {
            using (var enumA = colA.GetEnumerator())
            using (var enumB = colB.GetEnumerator())
            {
                while (enumA.MoveNext() && enumB.MoveNext())
                {
                    actions(enumA.Current, enumB.Current);
                }
            }
        }

        public static IEnumerable<Tuple<A, B>> ZipStrict<A, B>(this IEnumerable<A> colA, IEnumerable<B> colB)
        {
            using (var enumA = colA.GetEnumerator())
            using (var enumB = colB.GetEnumerator())
            {
                while (AssertoTwo(enumA.MoveNext(), enumB.MoveNext()))
                {
                    yield return new Tuple<A, B>(enumA.Current, enumB.Current);
                }
            }
        }

        public static IEnumerable<R> ZipStrict<A, B, R>(this IEnumerable<A> colA, IEnumerable<B> colB, Func<A, B, R> mixer)
        {
            using (var enumA = colA.GetEnumerator())
            using (var enumB = colB.GetEnumerator())
            {
                while (AssertoTwo(enumA.MoveNext(), enumB.MoveNext()))
                {
                    yield return mixer(enumA.Current, enumB.Current);
                }
            }
        }

        public static void ZipForeachStrict<A, B>(this IEnumerable<A> colA, IEnumerable<B> colB, Action<A, B> action)
        {
            using (var enumA = colA.GetEnumerator())
            using (var enumB = colB.GetEnumerator())
            {
                while (AssertoTwo(enumA.MoveNext(), enumB.MoveNext()))
                {
                    action(enumA.Current, enumB.Current);
                }
            }
        }

        static bool AssertoTwo(bool nextA, bool nextB)
        {
            if (nextA != nextB)
                if (nextA)
                    throw new InvalidOperationException(Resources.SecondCollectionsIsShorter);
                else
                    throw new InvalidOperationException(Resources.FirstCollectionIsShorter);
            else
                return nextA;
        }
        #endregion

        #region Conversions


        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        public static ReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> collection)
        {
            return collection == null ? null :
                collection as ReadOnlyCollection<T> ?? (collection as List<T> ?? collection.ToList()).AsReadOnly();
        }

        public static IEnumerable<T> AsThreadSafe<T>(this IEnumerable<T> source)
        {
            return new TreadSafeEnumerator<T>(source);
        }

        public static IEnumerable<T> ToProgressEnumerator<T>(this IEnumerable<T> source, out IProgressInfo pi)
        {
            pi = new ProgressEnumerator<T>(source, source.Count());
            return (IEnumerable<T>)pi;
        }



        public static void PushRange<T>(this Stack<T> stack, IEnumerable<T> elements)
        {
            foreach (var item in elements)
                stack.Push(item);
        }

        public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> elements)
        {
            foreach (var item in elements)
                queue.Enqueue(item);
        }

        public static void AddRange<T>(this HashSet<T> hashset, IEnumerable<T> coleccion)
        {
            foreach (var item in coleccion)
            {
                hashset.Add(item);
            }
        }

        public static bool TryContains<T>(this HashSet<T> hashset, T element)
        {
            if (hashset == null)
                return false;

            return hashset.Contains(element);
        }
        #endregion

        public static IEnumerable<R> JoinStrict<K, O, N, R>(
           IEnumerable<O> oldCollection,
           IEnumerable<N> newCollection,
           Func<O, K> oldKeySelector,
           Func<N, K> newKeySelector,
           Func<O, N, R> resultSelector, string action)
        {

            var oldDictionary = oldCollection.ToDictionary(oldKeySelector);
            var newDictionary = newCollection.ToDictionary(newKeySelector);

            var oldOnly = oldDictionary.Keys.Where(k => !newDictionary.ContainsKey(k)).ToList();
            var newOnly = newDictionary.Keys.Where(k => !oldDictionary.ContainsKey(k)).ToList();

            if (oldOnly.Count != 0)
                if (newOnly.Count != 0)
                    throw new InvalidOperationException(Resources.Error0Lacking1Extra2.Formato(action, newOnly.ToString(", "), oldOnly.ToString(", ")));
                else
                    throw new InvalidOperationException(Resources.Error0Extra1.Formato(action, oldOnly.ToString(", ")));
            else
                if (newOnly.Count != 0)
                    throw new InvalidOperationException(Resources.Error0Lacking1.Formato(action, newOnly.ToString(", ")));

            return oldDictionary.Select(p => resultSelector(p.Value, newDictionary[p.Key]));
        }
    }

    public enum BiSelectOptions
    {
        None,
        Initial,
        Final,
        InitialAndFinal,
        Circular,
    }
}
