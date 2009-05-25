using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Signum.Engine.Linq
{
    [DebuggerStepThrough]
    public static class ExpressionVisitorHelper
    {
        public static ReadOnlyCollection<T> NewIfChange<T>( this ReadOnlyCollection<T> collection, Func<T,T> newValue)
            where T:class
        {
            if (collection == null)
                return null; 

            List<T> alternate = null;
            for (int i = 0, n = collection.Count; i < n; i++)
            {
                T item = collection[i];
                T newItem = newValue(item);
                if (alternate == null && item != newItem)
                {
                    alternate = collection.Take(i).ToList();
                }
                if (alternate != null && newItem != null)
                {
                    alternate.Add(newItem);
                }
            }
            if (alternate != null)
            {
                return alternate.AsReadOnly();
            }
            return collection;
        }

        public static List<T> NewIfChange<T>(this List<T> collection, Func<T, T> newValue)
          where T : class
        {
            if (collection == null)
                return null;

            List<T> alternate = null;
            for (int i = 0, n = collection.Count; i < n; i++)
            {
                T item = collection[i];
                T newItem = newValue(item);
                if (alternate == null && item != newItem)
                {
                    alternate = collection.Take(i).ToList();
                }
                if (alternate != null && newItem != null)
                {
                    alternate.Add(newItem);
                }
            }
            return alternate ?? collection; 
        }
    }
}
