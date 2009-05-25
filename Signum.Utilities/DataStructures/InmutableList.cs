using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;
using System.Diagnostics;
using Signum.Utilities.Properties;

namespace Signum.Utilities.DataStructures
{
    [DebuggerTypeProxy(typeof(Proxy))]
    public abstract class InmutableList<T> : IEnumerable<T>
    {
        class ElementList: InmutableList<T>
        {
            readonly T value;
            readonly InmutableList<T> next;

            public ElementList(T value, InmutableList<T> next)
            {
                this.value = value;
                this.next = next;
            }

            public override T Value
            {
                get { return value; }
            }

            public override InmutableList<T> Next
            {
                get { return next; }
            }
        }

        class EmptyList : InmutableList<T>
        { 
            public override T Value
            {
                get { throw new InvalidOperationException(Resources.EmptyList); }
            }

            public override InmutableList<T> Next
            {
                get { throw new InvalidOperationException(Resources.EmptyList); }
            }
        }

        public static readonly InmutableList<T> Empty = new EmptyList();

        public abstract T Value { get; }

        public abstract InmutableList<T> Next { get; }

        public bool Contains(T element)
        {
            for (var node = this; node != Empty; node = node.Next)
                if (EqualityComparer<T>.Default.Equals(node.Value, element))
                    return true;

            return false; 
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var node = this; node != Empty; node = node.Next)
                yield return node.Value;
        }

        public InmutableList<T> And(T value)
        {
            return new ElementList(value, this);             
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }


    internal class Grouping<K, T> : List<T>, IGrouping<K, T>
    {
        K key;
        private Grouping() { }

        public static IGrouping<K, T> New(K key, IEnumerable<T> values)
        {
            var result = new Grouping<K, T> { key = key };
            result.AddRange(values);
            return result;
        }

        public K Key
        {
            get { return this.key; }
        }
    }


    internal class Proxy
    {
        public List<object> List;

        public Proxy(IEnumerable bla)
        {
            List = new List<object>( bla.Cast<object>());
        }
    }
}
