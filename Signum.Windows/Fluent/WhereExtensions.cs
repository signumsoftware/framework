using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Signum.Utilities;
using System.Windows.Media;
using System.Collections;
using System.Windows.Controls;

namespace Signum.Windows
{
    public static class WhereExtensions
    {
        public static IEnumerable<DependencyObject> VisualParents(this DependencyObject child)
        {
            return child.Follow(VisualTreeHelper.GetParent);
        }

        public static IEnumerable<DependencyObject> LogicalParents(this DependencyObject child)
        {
            return child.Follow(LogicalTreeHelper.GetParent);
        }

        public static IEnumerable<DependencyObject> BreathFirstVisual(DependencyObject parent, bool startOnParent, bool recursive, Func<DependencyObject, bool> predicate)
        {
            //http://en.wikipedia.org/wiki/Breadth-first_search
            Queue<DependencyObject> st = new Queue<DependencyObject>();
            if (startOnParent)
                st.Enqueue(parent);
            else
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = count - 1; i >= 0; i--)
                {
                    st.Enqueue(VisualTreeHelper.GetChild(parent, i));
                }
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

                int count = VisualTreeHelper.GetChildrenCount(dp);
                for (int i = 0; i < count; i++)
                {
                    st.Enqueue(VisualTreeHelper.GetChild(dp, i));
                }
            }
            yield break;
        }

        public static IEnumerable<DependencyObject> BreathFirstLogical(DependencyObject parent, bool startOnParent, bool recursive, Func<DependencyObject, bool> predicate)
        {
            //http://en.wikipedia.org/wiki/Breadth-first_search
            Queue<DependencyObject> st = new Queue<DependencyObject>();
            if (startOnParent)
                st.Enqueue(parent);
            else
                foreach (var item in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
                    st.Enqueue(item);

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


        public static IEnumerable<DependencyObject> DepthFirstVisual(DependencyObject parent, bool startOnParent, bool recursive, Func<DependencyObject, bool> predicate)
        {
            Stack<DependencyObject> st = new Stack<DependencyObject>();
            if (startOnParent)
                st.Push(parent);
            else
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = count - 1; i >= 0; i--)
                {
                    st.Push(VisualTreeHelper.GetChild(parent, i));
                }
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

                int count = VisualTreeHelper.GetChildrenCount(dp);
                for (int i = count - 1; i >= 0; i--)
                {
                    st.Push(VisualTreeHelper.GetChild(dp, i));
                }
            }
        }

        public static IEnumerable<DependencyObject> DepthFirstLogical(DependencyObject parent, bool startOnParent, bool recursive, Func<DependencyObject, bool> predicate)
        {
            Stack<DependencyObject> st = new Stack<DependencyObject>();
            if (startOnParent)
                st.Push(parent);
            else
                foreach (var item in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
                    st.Push(item);

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
            return Children<T>(parent,(Func<T,bool>)null, WhereFlags.Default).FirstEx();
        }

        public static T Child<T>(this DependencyObject parent, WhereFlags flags)
            where T : DependencyObject
        {
            return Children<T>(parent, (Func<T, bool>)null, flags).FirstEx();
        }

        public static T Child<T>(this DependencyObject parent, string route)
            where T : DependencyObject
        {
            return Children<T>(parent, p=>p.GetRoute() == route, WhereFlags.Default).FirstEx();
        }

        public static T Child<T>(this DependencyObject parent, Func<T, bool> predicate)
            where T : DependencyObject
        {
            return Children<T>(parent, predicate, WhereFlags.Default).FirstEx();
        }

        public static T Child<T>(this DependencyObject parent, string route, WhereFlags flags)
            where T : DependencyObject
        {
            return Children<T>(parent, p => p.GetRoute() == route, flags).FirstEx();
        }

        public static T Child<T>(this DependencyObject parent, Func<T, bool> predicate, WhereFlags flags)
            where T : DependencyObject
        {
            return Children<T>(parent, predicate, flags).FirstEx();
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

        public static IEnumerable<T> Children<T>(this DependencyObject parent, Func<T, bool> predicate, WhereFlags flags)
            where T : DependencyObject
        {
            bool depthFirst = (flags & WhereFlags.DepthFirst) == WhereFlags.DepthFirst;
            bool recursive = (flags & WhereFlags.Recursive) == WhereFlags.Recursive;
            bool visualTree = (flags & WhereFlags.VisualTree) == WhereFlags.VisualTree;
            bool startOnParent = (flags & WhereFlags.StartOnParent) == WhereFlags.StartOnParent;

            Func<DependencyObject, bool> finalPredicate;
            if (predicate == null)
                finalPredicate = (depObj => depObj is T);
            else
                finalPredicate = depObj => { T elem = depObj as T; return elem != null && predicate(elem); };

            if (visualTree)
            {
                if (depthFirst)
                    return DepthFirstVisual(parent, startOnParent, recursive, finalPredicate).Cast<T>();
                else
                    return BreathFirstVisual(parent, startOnParent, recursive, finalPredicate).Cast<T>();
            }
            else
            {
                if (depthFirst)
                    return DepthFirstLogical(parent, startOnParent, recursive, finalPredicate).Cast<T>();
                else
                    return BreathFirstLogical(parent, startOnParent, recursive, finalPredicate).Cast<T>();
            }
        }

        public static string DataContextBrokenBinding(this DependencyObject dep)
        {
            StringBuilder sb = new StringBuilder();

            FindDataContextBrokenBindings(0, dep, sb);

            return sb.ToString();
        }

        static bool FindDataContextBrokenBindings(int depth, DependencyObject dep, StringBuilder sb)
        {
            var fe = dep as FrameworkElement;
            var any = false;
            for (int i = 0; i < depth; i++)
                sb.Append(" ");
            sb.Append(dep.GetType().Name);

            if (fe != null && !IsTrivial(fe))
            {
                var source = DependencyPropertyHelper.GetValueSource(fe, FrameworkElement.DataContextProperty);

                if (source.BaseValueSource != BaseValueSource.Inherited)
                {
                    sb.AppendFormat(" .DataContext source {0}{1}: {2} ({3})",
                        source.BaseValueSource,
                        source.IsExpression ? " (IsExpression)" : "",
                        fe.DataContext?.ToString() ?? "null",
                        fe.DataContext?.Let(d => "(" + d.GetType().Name + " " + d.GetHashCode()));

                    any = true;
                }
            }

            int length = sb.Length;

            sb.AppendLine();

            int count = VisualTreeHelper.GetChildrenCount(dep);
           
            for (int i = 0; i < count; i++)
            {
                any |= FindDataContextBrokenBindings(depth + 1, VisualTreeHelper.GetChild(dep, i), sb);
            }

            if (!any)
            {
                sb.Remove(length, sb.Length - length);
                sb.AppendLine(" (...)");
            }

            return any;
        }

        private static bool IsTrivial(FrameworkElement fe)
        {
            return fe is ContentPresenter && VisualTreeHelper.GetChildrenCount(fe) == 1 &&
                VisualTreeHelper.GetChild(fe, 0).Let(a => a is AccessText || a is TextBlock);
        }
    }

    public enum WhereFlags
    {
        Default = NonRecursive | BreathFirst | LogicalTree | StartOnChildren,
        NonRecursive = 0,
        Recursive = 1,
        BreathFirst = 0,
        DepthFirst = 2,
        LogicalTree = 0,
        VisualTree = 4,
        StartOnChildren = 0,
        StartOnParent = 8
    }
}
