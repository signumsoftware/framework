using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.DataStructures;
using System.Threading;
using System.Globalization;

namespace Signum.Utilities
{
    public static class Sync
    {
        public static IDisposable ChangeCulture(string cultureName)
        {
            Thread t = Thread.CurrentThread;
            CultureInfo old = t.CurrentCulture;
            t.CurrentCulture = new CultureInfo(cultureName);
            return new Disposable(() => t.CurrentCulture = old);
        }

        public static T Initialize<T>(ref T variable, Func<T> initialize) where T : class
        {
            return variable ?? (variable = initialize());
        }

        public static void SafeUpdate<T>(ref T variable, Func<T, T> repUpdateFunction) where T : class
        {
            T oldValue, newValue;
            do
            {
                oldValue = variable;
                newValue = repUpdateFunction(oldValue);

                if (newValue == null)
                    break;

            } while (Interlocked.CompareExchange<T>(ref variable, newValue, oldValue) != oldValue);
        }

        public static V SafeGetOrCreate<K, V>(ref ImmutableAVLTree<K, V> tree, K key, Func<V> createValue)
            where K : IComparable<K>
        {
            V result;
            if (tree.TryGetValue(key, out result))
                return result;

            V value = createValue();

            SafeUpdate(ref tree, t =>
            {
                if (t.TryGetValue(key, out result))
                    return null;
                else
                {
                    result = value;
                    return t.Add(key, value);
                }
            });

            return result;
        }

    }
}
