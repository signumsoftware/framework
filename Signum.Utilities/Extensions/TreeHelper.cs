using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Signum.Utilities
{
    public static class TreeHelper
    {
        public static ObservableCollection<Node<T>> ToTreeC<T>(IEnumerable<T> collection, Func<T, T> getParent)
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

        public static ObservableCollection<Node<T>> ToTreeS<T>(IEnumerable<T> collection, Func<T, T?> getParent)
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
                stack.PushRange(children(elem).Reverse());
            }
        }

        public static ObservableCollection<Node<S>> SelectTree<T, S>(ObservableCollection<Node<T>> nodes, Func<T, S> selector)
        {
            return nodes.Select(n => new Node<S>(selector(n.Value), SelectTree(n.Children, selector))).ToObservableCollection();
        }

        public static ObservableCollection<Node<S>> SelectSimplifyTreeC<T, S>(ObservableCollection<Node<T>> nodes, Func<T, S> selector) where S : class
        {
            ObservableCollection<Node<S>> result = new ObservableCollection<Node<S>>();

            foreach (var item in nodes)
            {
                var newValue = selector(item.Value);

                if (newValue != null)
                    result.Add(new Node<S>(newValue, SelectSimplifyTreeC(item.Children, selector)));
                else
                    result.AddRange(SelectSimplifyTreeC(item.Children, selector));
            }

            return result;
        }

        public static ObservableCollection<Node<S>> SelectSimplifyTreeS<T, S>(ObservableCollection<Node<T>> nodes, Func<T, S?> selector) where S : struct
        {
            ObservableCollection<Node<S>> result = new ObservableCollection<Node<S>>();

            foreach (var item in nodes)
            {
                var newValue = selector(item.Value);

                if (newValue != null)
                    result.Add(new Node<S>(newValue.Value, SelectSimplifyTreeS(item.Children, selector)));
                else
                    result.AddRange(SelectSimplifyTreeS(item.Children, selector));
            }

            return result;
        }

        public static ObservableCollection<Node<T>> Apply<T>(ObservableCollection<Node<T>> collection, Func<ObservableCollection<Node<T>>, ObservableCollection<Node<T>>> action)
        {
            return action(collection).Select(a => new Node<T>(a.Value, Apply(a.Children, action))).ToObservableCollection();
        }
    }

    public class Node<T> : INotifyPropertyChanged
    {
        public T Value { get; set; }
        public ObservableCollection<Node<T>> Children { get; set; }

        public Node(T value, ObservableCollection<Node<T>> children)
        {
            Value = value;
            Children = children ?? new ObservableCollection<Node<T>>();
        }

        public Node(T value)
        {
            Value = value;
            Children = new ObservableCollection<Node<T>>();
        }

        public Node()
        {
            Children = new ObservableCollection<Node<T>>();
        }


        public override string ToString()
        {
            return "{0} Children: {1}".FormatWith(Children.Count, Value);
        }

        void Never()
        {
            PropertyChanged(null, null); 
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
