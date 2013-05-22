using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Signum.Utilities.DataStructures
{

    public class ImmutableAVLTree<K, V> where K : IComparable<K>
    {
        private class ImmutableFullAVLTree : ImmutableAVLTree<K, V>
        {
            readonly K key;
            readonly V value;
            readonly int height;
            readonly ImmutableAVLTree<K, V> left;
            readonly ImmutableAVLTree<K, V> right;

            public ImmutableFullAVLTree(K key, V value, ImmutableAVLTree<K, V> left, ImmutableAVLTree<K, V> right)
            {
                this.key = key;
                this.value = value;
                this.left = left;
                this.right = right;
                this.height = 1 + Math.Max(left.Height, right.Height);
            }

            public override bool IsEmpty { get { return false; } }
            public override K Key { get { return key; } }
            public override V Value { get { return value; } }
            public override int Height { get { return height; } }

            public override ImmutableAVLTree<K, V> Left { get { return left; } }
            public override ImmutableAVLTree<K, V> Right { get { return right; } }

            public override ImmutableAVLTree<K, V> Add(K key, V value)
            {
                ImmutableAVLTree<K, V> result;
                if (key.CompareTo(Key) > 0)
                    result = new ImmutableFullAVLTree(Key, Value, Left, Right.Add(key, value));
                else
                    result = new ImmutableFullAVLTree(Key, Value, Left.Add(key, value), Right);
                return MakeBalanced(result);
            }

            public override ImmutableAVLTree<K, V> Remove(K key)
            {
                ImmutableAVLTree<K, V> result;
                int compare = key.CompareTo(Key);
                if (compare == 0)
                {
                    // We have a match. If this is a leaf, just remove it 
                    // by returning Empty.  If we have only one child,
                    // replace the node with the child.
                    if (Right.IsEmpty && Left.IsEmpty)
                        result = Empty;
                    else if (Right.IsEmpty && !Left.IsEmpty)
                        result = Left;
                    else if (!Right.IsEmpty && Left.IsEmpty)
                        result = Right;
                    else
                    {
                        // We have two children. Remove the next-highest node and replace
                        // this node with it.
                        ImmutableAVLTree<K, V> successor = Right;
                        while (!successor.Left.IsEmpty)
                            successor = successor.Left;
                        result = new ImmutableFullAVLTree(successor.Key, successor.Value, Left, Right.Remove(successor.Key));
                    }
                }
                else if (compare < 0)
                    result = new ImmutableFullAVLTree(Key, Value, Left.Remove(key), Right);
                else
                    result = new ImmutableFullAVLTree(Key, Value, Left, Right.Remove(key));
                return MakeBalanced(result);
            }

            public override ImmutableAVLTree<K, V> Search(K key)
            {
                int compare = key.CompareTo(Key);
                if (compare == 0)
                    return this;
                else if (compare > 0)
                    return Right.Search(key);
                else
                    return Left.Search(key);
            }

            public override bool Contains(K key) { return !Search(key).IsEmpty; }

            public override IEnumerable<K> Keys
            {
                get
                {
                    foreach (var tree in Enumerate())
                        yield return tree.Key;
                }
            }

            public override IEnumerable<V> Values
            {
                get
                {
                    foreach (var tree in Enumerate())
                        yield return tree.Value;
                }
            }

            public override IEnumerable<KeyValuePair<K, V>> Pairs
            {
                get
                {
                    foreach (var tree in Enumerate())
                        yield return new KeyValuePair<K, V>(tree.Key, tree.Value);
                }
            }

            public override string ToString()
            {
                return Enumerate().ToString(t => "[{0},{1}]".Formato(t.Key, t.Value), "\r\n");
            }

            IEnumerable<ImmutableAVLTree<K, V>> Enumerate()
            {
                var stack = ImmutableStack<ImmutableAVLTree<K, V>>.Empty.Push(this);

                bool left = true; 
                while (!stack.IsEmpty)
                {
                    var node = stack.Peek();
                    if (left)
                    {
                        if (!node.Left.IsEmpty)
                            stack = stack.Push(node.Left);
                        else
                            left = false;
                    }
                    else
                    {
                        stack = stack.Pop();
                        yield return node;
                        if (!node.Right.IsEmpty)
                        {
                            stack = stack.Push(node.Right);
                            left = true; 
                        }
                    }
                }
            }

        }

        static readonly ImmutableAVLTree<K, V> empty = new ImmutableAVLTree<K, V>();
        public static ImmutableAVLTree<K, V> Empty { get { return empty; } }

        private ImmutableAVLTree() { }

        public virtual bool IsEmpty { get { return true; } }
        public virtual K Key { get { throw new InvalidOperationException("Empty Tree"); } }
        public virtual V Value { get { throw new InvalidOperationException("Empty Tree"); } }
        public virtual int Height { get { return 0; } }

        public virtual ImmutableAVLTree<K, V> Left { get { throw new InvalidOperationException("Empty Tree"); } }
        public virtual ImmutableAVLTree<K, V> Right { get { throw new InvalidOperationException("Empty Tree"); } }

        public virtual ImmutableAVLTree<K, V> Add(K key, V value) { return new ImmutableFullAVLTree(key, value, this, this); }
        public virtual ImmutableAVLTree<K, V> Remove(K key) { throw new InvalidOperationException("Cannot remove item that is not in tree."); }

        public virtual ImmutableAVLTree<K, V> Search(K key) { return this; }
        public virtual bool Contains(K key) { return false; }

        public virtual IEnumerable<K> Keys { get { yield break; } }
        public virtual IEnumerable<V> Values { get { yield break; } }
        public virtual IEnumerable<KeyValuePair<K, V>> Pairs { get { yield break; } }

        public V this[K key]
        {
            get
            {
                ImmutableAVLTree<K, V> tree = Search(key);
                if (tree.IsEmpty)
                    throw new KeyNotFoundException("Key {0} not found".Formato(key));
                return tree.Value;
            }
        }

        public bool TryGetValue(K key, out V value)
        {
            ImmutableAVLTree<K, V> tree = Search(key);
            if (tree.IsEmpty)
            {
                value = default(V);
                return false;
            }
            value = tree.Value;
            return true;
        }

        public override string ToString() { return "{Empty}"; }

        static ImmutableAVLTree<K, V> RotateLeft(ImmutableAVLTree<K, V> tree)
        {
            if (tree.Right.IsEmpty)
                return tree;
            return new ImmutableFullAVLTree(tree.Right.Key, tree.Right.Value,
                new ImmutableFullAVLTree(tree.Key, tree.Value, tree.Left, tree.Right.Left),
                tree.Right.Right);
        }

        static ImmutableAVLTree<K, V> RotateRight(ImmutableAVLTree<K, V> tree)
        {
            if (tree.Left.IsEmpty)
                return tree;
            return new ImmutableFullAVLTree(tree.Left.Key, tree.Left.Value, tree.Left.Left,
                new ImmutableFullAVLTree(tree.Key, tree.Value, tree.Left.Right, tree.Right));
        }

        static ImmutableAVLTree<K, V> DoubleLeft(ImmutableAVLTree<K, V> tree)
        {
            if (tree.Right.IsEmpty)
                return tree;
            ImmutableAVLTree<K, V> rotatedRightChild = new ImmutableFullAVLTree(tree.Key, tree.Value, tree.Left, RotateRight(tree.Right));
            return RotateLeft(rotatedRightChild);
        }

        static ImmutableAVLTree<K, V> DoubleRight(ImmutableAVLTree<K, V> tree)
        {
            if (tree.Left.IsEmpty)
                return tree;
            ImmutableAVLTree<K, V> rotatedLeftChild = new ImmutableFullAVLTree(tree.Key, tree.Value, RotateLeft(tree.Left), tree.Right);
            return RotateRight(rotatedLeftChild);
        }

        static int Balance(ImmutableAVLTree<K, V> tree)
        {
            if (tree.IsEmpty)
                return 0;
            return tree.Right.Height - tree.Left.Height;
        }

        static bool IsRightHeavy(ImmutableAVLTree<K, V> tree)
        {
            return Balance(tree) >= 2;
        }

        static bool IsLeftHeavy(ImmutableAVLTree<K, V> tree)
        {
            return Balance(tree) <= -2;
        }

        static ImmutableAVLTree<K, V> MakeBalanced(ImmutableAVLTree<K, V> tree)
        {
            ImmutableAVLTree<K, V> result;
            if (IsRightHeavy(tree))
            {
                if (IsLeftHeavy(tree.Right))
                    result = DoubleLeft(tree);
                else
                    result = RotateLeft(tree);
            }
            else if (IsLeftHeavy(tree))
            {
                if (IsRightHeavy(tree.Left))
                    result = DoubleRight(tree);
                else
                    result = RotateRight(tree);
            }
            else
                result = tree;
            return result;
        }
    }
} 


