using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Utilities
{
    public static class TreeHelper
    {
        public static List<Node<T>> ToTreeC<T>(IEnumerable<T> collection, Func<T, T> getParent)
            where T: class 
        {
            Node<T> top = new Node<T>();

            Dictionary<T,Node<T>> dic =new Dictionary<T,Node<T>>();

            Func<T, Node<T>> createNode = null;

            createNode = item => dic.GetOrCreate(item, ()=>
                {
                    Node<T> itemNode = new Node<T>(item);
                    T parent = getParent(item);
                    Node<T> parentNode = parent != null? createNode(parent) : top;
                    parentNode.Childs.Add(itemNode);
                    return itemNode;
                }); 

            foreach (var item in collection)
            {
                createNode(item); 
            }

            return top.Childs; 
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
                parentNode.Childs.Add(itemNode);
                return itemNode;
            });

            foreach (var item in collection)
            {
                createNode(item);
            }

            return top.Childs;
        }

        public static IEnumerable<T> BreathFirst<T>(T root, Func<T, IEnumerable<T>> childs)
        {
            Stack<T> stack = new Stack<T>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                T elem = stack.Pop();
                yield return elem;
                stack.PushRange(childs(elem));
            }
        }
    }

    public class Node<T>
    {
        public T Value { get; set; }
        public List<Node<T>> Childs { get; set; }

        public Node(T value)
        {
            Value = value;
            Childs = new List<Node<T>>(); 
        }

        public Node()
        {
            Childs = new List<Node<T>>(); 
        }
    }
}
