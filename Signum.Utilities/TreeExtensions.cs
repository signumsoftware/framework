using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Utilities
{
    public static class TreeHelper
    {
        public static List<Node<T>> ToTreeC<T>(IEnumerable<T> collection, Func<T, T> getParent)
            where T : class
        {
            Node<T> top = new Node<T>();

            Dictionary<T, Node<T>> dic = new Dictionary<T, Node<T>>();

            Func<T, Node<T>> createNode = null;

            createNode = item => dic.GetOrCreate(item, () =>
                {
                    Node<T> itemNode = new Node<T>(item);
                    T parent = getParent(item);
                    Node<T> parentNode = parent != null ? createNode(parent) : top;
                    parentNode.Children.Add(itemNode);
                    return itemNode;
                });

            foreach (var item in collection)
            {
                createNode(item);
            }

            return top.Children;
        }

        public static List<Node<T>> ToTreeS<T>(IEnumerable<T> collection, Func<T, T?> getParent)
            where T : struct
        {
            Node<T> top = new Node<T>();

            Dictionary<T, Node<T>> dic = new Dictionary<T, Node<T>>();

            Func<T, Node<T>> createNode = null;

            createNode = item => dic.GetOrCreate(item, () =>
            {
                Node<T> itemNode = new Node<T>(item);
                T? parent = getParent(item);
                Node<T> parentNode = parent != null ? createNode(parent.Value) : top;
                parentNode.Children.Add(itemNode);
                return itemNode;
            });

            foreach (var item in collection)
            {
                createNode(item);
            }

            return top.Children;
        }

        public static IEnumerable<T> BreathFirst<T>(T root, Func<T, IEnumerable<T>> children)
        {
            Queue<T> stack = new Queue<T>();
            stack.Enqueue(root);
            while (stack.Count > 0)
            {
                T elem = stack.Dequeue();
                yield return elem;
                stack.EnqueueRange(children(elem));
            }
        }

        public static IEnumerable<T> DepthFirst<T>(T root, Func<T, IEnumerable<T>> children)
        {
            Stack<T> stack = new Stack<T>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                T elem = stack.Pop();
                yield return elem;
                stack.PushRange(children(elem));
            }
        }

        public static List<Node<S>> SelectTree<T, S>(List<Node<T>> roots, Func<T, S> selector)
        {
            return roots.Select(n => new Node<S>(selector(n.Value)) { Children = SelectTree(n.Children, selector) }).ToList();
        }
    }

    public class Node<T>
    {
        public T Value { get; set; }
        public List<Node<T>> Children { get; set; }

        public Node(T value)
        {
            Value = value;
            Children = new List<Node<T>>();
        }

        public Node()
        {
            Children = new List<Node<T>>();
        }

        //public override string ToString()
        //{
        //   // StringWriter 
        //}
    }
}
