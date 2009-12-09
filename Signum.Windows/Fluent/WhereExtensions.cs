using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Signum.Utilities;
using System.Windows.Media;

namespace Signum.Windows
{
    public static class WhereExtensions
    {

        public static IEnumerable<DependencyObject> Parents(this DependencyObject child)
        {
            return child.FollowC(VisualTreeHelper.GetParent);
        }


        public static IEnumerable<DependencyObject> BreathFirst(DependencyObject parent, bool recursive, Func<DependencyObject, bool> predicate)
        {
            //http://en.wikipedia.org/wiki/Breadth-first_search
            Queue<DependencyObject> st = new Queue<DependencyObject>();
            st.Enqueue(parent);

            while (st.Count > 0)
            {
                DependencyObject dp = st.Dequeue();

                if (predicate(dp))
                {
                    yield return dp;
                    if (!recursive)
                        continue;
                }

                int count = VisualTreeHelper.GetChildrenCount(dp);
                for (int i = 0; i < count; i++)
                {
                    st.Enqueue(VisualTreeHelper.GetChild(dp, i));
                }
            }
            yield break;
        }

        public static IEnumerable<DependencyObject> DepthFirst(DependencyObject parent, bool recursive, Func<DependencyObject, bool> predicate)
        {
            Stack<DependencyObject> st = new Stack<DependencyObject>();
            st.Push(parent);
            while (st.Count > 0)
            {
                DependencyObject dp = st.Pop();
                if (predicate(dp))
                {
                    yield return dp;

                    if (!recursive)
                        continue;
                }

                int count = VisualTreeHelper.GetChildrenCount(dp);
                for (int i = count - 1; i >= 0; i--)
                {
                    st.Push(VisualTreeHelper.GetChild(dp, i));
                }
            }
        }

        public enum WhereFlags
        {
            Default = NonRecursive | BreathFirst,
            NonRecursive = 0,
            Recursive = 1,
            BreathFirst = 0,
            DepthFirst = 2,
        }


        public static IEnumerable<T> Childs<T>(this DependencyObject parent)
            where T : DependencyObject
        {
            return Childs<T>(parent, null, WhereFlags.Default);
        }

        public static IEnumerable<T> Childs<T>(this DependencyObject parent, WhereFlags flags)
            where T : DependencyObject
        {
            return Childs<T>(parent, null, flags);
        }

        public static IEnumerable<T> Childs<T>(this DependencyObject parent, Func<T, bool> predicate)
            where T : DependencyObject
        {
            return Childs<T>(parent, predicate, WhereFlags.Default);
        }

        public static IEnumerable<T> Childs<T>(this DependencyObject parent, Func<T, bool> predicate, WhereFlags flags)
            where T: DependencyObject
        {
            bool depthFirst = (flags & WhereFlags.DepthFirst) == WhereFlags.DepthFirst;
            bool recursive = (flags & WhereFlags.Recursive) == WhereFlags.Recursive;

            Func<DependencyObject, bool> finalPredicate;
            if (predicate == null)
                finalPredicate = (depObj => depObj is T);
            else
                finalPredicate = depObj => { T elem = depObj as T; return elem != null && predicate(elem); };

            if (depthFirst)
                return DepthFirst(parent, recursive, finalPredicate).Cast<T>();
            else
                return BreathFirst(parent, recursive, finalPredicate).Cast<T>();
        }
    }
}
