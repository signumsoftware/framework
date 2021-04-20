using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System.Collections.Immutable;

namespace Signum.Utilities.DataStructures
{
    public static class ImmutableStackExtensions
    {
        public static ImmutableStack<T> Reverse<T>(this ImmutableStack<T> stack) where T : notnull
        {
            return Reverse(stack, ImmutableStack<T>.Empty);
        }

        public static ImmutableStack<T> Reverse<T>(this ImmutableStack<T> stack, ImmutableStack<T> initial) where T : notnull
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
