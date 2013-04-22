using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Signum.Utilities.DataStructures
{
    public class ImmutableQueue<T> : IEnumerable<T>
    {
        private class ImmutableFullQueue : ImmutableQueue<T>
        {
            readonly ImmutableStack<T> backwards;
            readonly ImmutableStack<T> forwards;

            public ImmutableFullQueue(ImmutableStack<T> f, ImmutableStack<T> b)
            {
                forwards = f;
                backwards = b;
            }

            public override bool IsEmpty { get { return false; } }
            public override T Peek() { return forwards.Peek(); }

            public override ImmutableQueue<T> Enqueue(T value)
            {
                return new ImmutableFullQueue(forwards, backwards.Push(value));
            }

            public override ImmutableQueue<T> Dequeue()
            {
                ImmutableStack<T> f = forwards.Pop();
                if (!f.IsEmpty)
                    return new ImmutableFullQueue(f, backwards);
                else if (backwards.IsEmpty)
                    return ImmutableQueue<T>.Empty;
                else
                    return new ImmutableFullQueue(backwards.Reverse(), ImmutableStack<T>.Empty);
            }

            public override IEnumerator<T> GetEnumerator()
            {
                foreach (T t in forwards) yield return t;
                foreach (T t in backwards.Reverse()) yield return t;
            }

            public override string ToString()
            {
                return "[" + this.ToString(", ") + "]";
            }

        }

        static readonly ImmutableQueue<T> empty = new ImmutableQueue<T>();
        public static ImmutableQueue<T> Empty { get { return empty; } }

        private ImmutableQueue() { }

        public virtual bool IsEmpty { get { return true; } }
        public virtual T Peek() { throw new InvalidOperationException("Empty queue"); }
        public virtual ImmutableQueue<T> Enqueue(T value)
        {
            return new ImmutableFullQueue(ImmutableStack<T>.Empty.Push(value), ImmutableStack<T>.Empty);
        }

        public virtual ImmutableQueue<T> Dequeue() { throw new InvalidOperationException("Empty queue"); }
        public virtual IEnumerator<T> GetEnumerator() { yield break; }
        public override string ToString() { return "[]"; }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }
}
