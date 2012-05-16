using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Signum.Utilities;
using System.Windows.Media;
using System.Collections;

namespace Signum.Windows
{
    public static class WhereExtensions
    {
        public static IEnumerable<DependencyObject> VisualParents(this DependencyObject child)
        {
            return child.FollowC(VisualTreeHelper.GetParent);
        }

        public static IEnumerable<DependencyObject> LogicalParents(this DependencyObject child)
        {
            return child.FollowC(LogicalTreeHelper.GetParent);
        }

        public static IEnumerable<DependencyObject> BreathFirstVisual(DependencyObject parent, bool recursive, Func<DependencyObject, bool> predicate)
        {
            //http://en.wikipedia.org/wiki/Breadth-first_search
            Queue<DependencyObject> st = new Queue<DependencyObject>();
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                st.Enqueue(VisualTreeHelper.GetChild(parent, i));
            }

            while (st.Count > 0)
            {
                DependencyObject dp = st.Dequeue();

                if (predicate(dp))
                {
                    yield return dp;
                    if (!recursive)
                        continue;
                }

                count = VisualTreeHelper.GetChildrenCount(dp);
                for (int i = 0; i < count; i++)
                {
                    st.Enqueue(VisualTreeHelper.GetChild(dp, i));
                }
            }
            yield break;
        }

        public static IEnumerable<DependencyObject> BreathFirstLogical(DependencyObject parent, bool recursive, Func<DependencyObject, bool> predicate)
        {
            //http://en.wikipedia.org/wiki/Breadth-first_search
            Queue<DependencyObject> st = new Queue<DependencyObject>(
                LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>());

            while (st.Count > 0)
            {
                DependencyObject dp = st.Dequeue();

                if (predicate(dp))
                {
                    yield return dp;
                    if (!recursive)
                        continue;
                }

                foreach (DependencyObject d in LogicalTreeHelper.GetChildren(dp).OfType<DependencyObject>())
                {
                    st.Enqueue(d);
                }           
            }
            yield break;
        }


        public static IEnumerable<DependencyObject> DepthFirstVisual(DependencyObject parent, bool recursive, Func<DependencyObject, bool> predicate)
        {
            Stack<DependencyObject> st = new Stack<DependencyObject>();
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = count - 1; i >= 0; i--)
            {
                st.Push(VisualTreeHelper.GetChild(parent, i));
            }

            while (st.Count > 0)
            {
                DependencyObject dp = st.Pop();
                if (predicate(dp))
                {
                    yield return dp;

                    if (!recursive)
                        continue;
                }

                count = VisualTreeHelper.GetChildrenCount(dp);
                for (int i = count - 1; i >= 0; i--)
                {
                    st.Push(VisualTreeHelper.GetChild(dp, i));
                }
            }
        }

        public static IEnumerable<DependencyObject> DepthFirstLogical(DependencyObject parent, bool recursive, Func<DependencyObject, bool> predicate)
        {
            Stack<DependencyObject> st = new Stack<DependencyObject>(
                LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>().Reverse());

            while (st.Count > 0)
            {
                DependencyObject dp = st.Pop();
                if (predicate(dp))
                {
                    yield return dp;

                    if (!recursive)
                        continue;
                }

                foreach (DependencyObject d in LogicalTreeHelper.GetChildren(dp).OfType<DependencyObject>().Reverse())
                {
                    st.Push(d);
                }      
            }
        }

        public static T Child<T>(this DependencyObject parent)
            where T : DependencyObject
        {
            return Children<T>(parent,(Func<T,bool>)null, WhereFlags.Default).First();
        }

        public static T Child<T>(this DependencyObject parent, WhereFlags flags)
            where T : DependencyObject
        {
            return Children<T>(parent, (Func<T, bool>)null, flags).First();
        }

        public static T Child<T>(this DependencyObject parent, string route)
            where T : DependencyObject
        {
            return Children<T>(parent, p=>p.GetRoute() == route, WhereFlags.Default).First();
        }

        public static T Child<T>(this DependencyObject parent, Func<T, bool> predicate)
            where T : DependencyObject
        {
            return Children<T>(parent, predicate, WhereFlags.Default).First();
        }

        public static T Child<T>(this DependencyObject parent, string route, WhereFlags flags)
            where T : DependencyObject
        {
            return Children<T>(parent, p => p.GetRoute() == route, flags).First();
        }

        public static T Child<T>(this DependencyObject parent, Func<T, bool> predicate, WhereFlags flags)
            where T : DependencyObject
        {
            return Children<T>(parent, predicate, flags).First();
        }



        public static IEnumerable<T> Children<T>(this DependencyObject parent)
            where T : DependencyObject
        {
            return Children<T>(parent, (Func<T, bool>)null, WhereFlags.Default);
        }

        public static IEnumerable<T> Children<T>(this DependencyObject parent, WhereFlags flags)
            where T : DependencyObject
        {
            return Children<T>(parent, (Func<T, bool>)null, flags);
        }

        public static IEnumerable<T> Children<T>(this DependencyObject parent, string route)
        where T : DependencyObject
        {
            return Children<T>(parent, p => p.GetRoute() == route, WhereFlags.Default);
        }

        public static IEnumerable<T> Children<T>(this DependencyObject parent, Func<T, bool> predicate)
            where T : DependencyObject
        {
            return Children<T>(parent, predicate, WhereFlags.Default);
        }

        public static IEnumerable<T> Children<T>(this DependencyObject parent, string route, WhereFlags flags)
        where T : DependencyObject
        {
            return Children<T>(parent, p => p.GetRoute() == route, flags);
        }


        public static IEnumerable Children(this DependencyObject parent, Func<object, bool> predicate, WhereFlags flags)
        {
            bool depthFirst = (flags & WhereFlags.DepthFirst) == WhereFlags.DepthFirst;
            bool recursive = (flags & WhereFlags.Recursive) == WhereFlags.Recursive;
            bool visualTree = (flags & WhereFlags.VisualTree) == WhereFlags.VisualTree;

            Func<DependencyObject, bool> finalPredicate;
            if (predicate == null)
                finalPredicate = (depObj => depObj is object);
            else
                finalPredicate = depObj => { object elem = depObj as object; return elem != null && predicate(elem); };

            if (visualTree)
            {
                if (depthFirst)
                    return DepthFirstVisual(parent, recursive, finalPredicate);
                else
                    return BreathFirstVisual(parent, recursive, finalPredicate);
            }
            else
            {
                if (depthFirst)
                    return DepthFirstLogical(parent, recursive, finalPredicate);
                else
                    return BreathFirstLogical(parent, recursive, finalPredicate);
            }
        }


        public static IEnumerable<T> Children<T>(this DependencyObject parent, Func<T, bool> predicate, WhereFlags flags)
            where T: DependencyObject
        {
            bool depthFirst = (flags & WhereFlags.DepthFirst) == WhereFlags.DepthFirst;
            bool recursive = (flags & WhereFlags.Recursive) == WhereFlags.Recursive;
            bool visualTree = (flags & WhereFlags.VisualTree) == WhereFlags.VisualTree;

            Func<DependencyObject, bool> finalPredicate;
            if (predicate == null)
                finalPredicate = (depObj => depObj is T);
            else
                finalPredicate = depObj => { T elem = depObj as T; return elem != null && predicate(elem); };

            if (visualTree)
            {
                if (depthFirst)
                    return DepthFirstVisual(parent, recursive, finalPredicate).Cast<T>();
                else
                    return BreathFirstVisual(parent, recursive, finalPredicate).Cast<T>();
            }
            else
            {
                if (depthFirst)
                    return DepthFirstLogical(parent, recursive, finalPredicate).Cast<T>();
                else
                    return BreathFirstLogical(parent, recursive, finalPredicate).Cast<T>();
            }
        }
    }

    public enum WhereFlags
    {
        Default = NonRecursive | BreathFirst | LogicalTree,
        NonRecursive = 0,
        Recursive = 1,
        BreathFirst = 0,
        DepthFirst = 2,
        LogicalTree = 0,
        VisualTree = 4
    }
}
