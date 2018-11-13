using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;

namespace Signum.Utilities.DataStructures
{
    public class ImmutableStack<T>:IEnumerable<T>
    {
        private class ImmutableFullStack : ImmutableStack<T>
        {
            readonly T head;
            readonly ImmutableStack<T> tail;

            public ImmutableFullStack(T head, ImmutableStack<T> tail)
            {
                this.head = head;
                this.tail = tail;
            }

            public override bool IsEmpty { get { return false; } }
            public override T Peek() { return head; }
            public override ImmutableStack<T> Pop() { return tail; }
            public override ImmutableStack<T> Push(T value) { return new ImmutableFullStack(value, this); }

            public override IEnumerator<T> GetEnumerator()
            {
                for (ImmutableStack<T> stack = this; !stack.IsEmpty; stack = stack.Pop())
                    yield return stack.Peek();
            }

            public override string ToString()
            {
                return "[" + this.ToString(", ") + "]";
            }

        }

        public static readonly ImmutableStack<T> Empty = new ImmutableStack<T>();

        private ImmutableStack(){}

        public virtual bool IsEmpty { get { return true; } }
        public virtual T Peek() { throw new InvalidOperationException("Empty Stack"); }
        public virtual ImmutableStack<T> Push(T value) { return new ImmutableFullStack(value, this); }
        public virtual ImmutableStack<T> Pop() { throw new InvalidOperationException("Empty Stack"); }
        public virtual IEnumerator<T> GetEnumerator() { yield break; }
        public override string ToString() { return "[]"; }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }

    public static class ImmutableStackExtensions
    {
        public static ImmutableStack<T> Reverse<T>(this ImmutableStack<T> stack)
        {
            return Reverse(stack, ImmutableStack<T>.Empty);
        }

        public static ImmutableStack<T> Reverse<T>(this ImmutableStack<T> stack, ImmutableStack<T> initial)
        {
            ImmutableStack<T> r = initial;
            for (ImmutableStack<T> f = stack; !f.IsEmpty; f = f.Pop())
                r = r.Push(f.Peek());
            return r;
        }

        public static void SafeUpdate<T>(ref T variable, Func<T, T> repUpdateFunction) where T : class
        {
            T oldValue, newValue;
            do
            {
                oldValue = variable;
                newValue = repUpdateFunction(oldValue);

                if (newValue == null)
                    break;

            } while (Interlocked.CompareExchange<T>(ref variable, newValue, oldValue) != oldValue);
        }
    }
}
