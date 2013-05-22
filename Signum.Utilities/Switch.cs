using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Signum.Utilities.ExpressionTrees; 

namespace Signum.Utilities
{
    [DebuggerStepThrough]
    public class Switch<T>
    {
        bool consumed = false;
        T value;

        public Switch(T value)
        {
            this.value = value;
            this.consumed = false;
        }

        public Switch<T> Case(T value, Action<T> action)
        {
            if (!consumed && object.Equals(this.value, value))
            {
                action(value);
                consumed = true;
            }
            return this;
        }

        public Switch<T> Case(Predicate<T> condition, Action<T> action)
        {
            if (!consumed && condition(this.value))
            {
                action(value);
                consumed = true;
            }
            return this; 
        }

        public Switch<T> Case<S>(Action<S> action)
            where S: T
        {
            if (!consumed && value is S)
            {
                action((S)value);
                consumed = true;
            }
            return this;
        }

        public void Default(Action<T> action)
        {
            if (!consumed)
            {
                action(value);
                consumed = true;
            }
        }
    }

    [DebuggerStepThrough]
    public class Switch<T,R>
    {
        bool consumed = false;
        T value;
        R result; 

        public Switch(T value)
        {
            this.value = value;
            this.consumed = false;
            this.result = default(R);
        }

        public Switch<T, R> Case(T value, R result)
        {
            if (!consumed && object.Equals(this.value, value))
            {
                this.result = result;
                this.consumed = true;
            }
            return this;
        }

        public Switch<T, R> Case(T value, Func<T, R> func)
        {
            if (!consumed && object.Equals(this.value, value))
            {
                this.result = func(value);
                this.consumed = true;
            }
            return this;
        }

        public Switch<T, R> Case(Predicate<T> condition, R result)
        {
            if (!consumed && condition(this.value))
            {
                this.result = result;
                this.consumed = true;
            }
            return this;
        }

        public Switch<T, R> Case(Predicate<T> condition, Func<T, R> func)
        {
            if (!consumed && condition(this.value))
            {
                this.result = func(value);
                this.consumed = true;
            }
            return this;
        }

        public Switch<T, R> Case<S>(Func<S, R> func) where S : T
        {
            if (!consumed && value is S)
            {
                this.result = func((S)value);
                this.consumed = true;
            }
            return this;
        }

        public Switch<T, R> Case<S>(R result) where S : T
        {
            if (!consumed && value is S)
            {
                this.result = result;
                this.consumed = true;
            }
            return this;
        }

        public R Default(Func<T, R> func)
        {
            if (!consumed)
            {
                this.result = func(value);
                this.consumed = true;
            }

            return this.result; 
        }

        public R Default(R result)
        {
            if (!consumed)
            {
                this.result = result;
                this.consumed = true;
            }

            return this.result;
        }

        public R NoDefault()
        {
            if (!consumed)
            {
                throw new InvalidOperationException("No match in {0} -> {1} switch".Formato(typeof(T).TypeName(), typeof(R).TypeName()));                 
            }

            return this.result;
        }
          
        public R NoDefault(string message)
        {
            if (!consumed)
            {
                throw new InvalidOperationException(message);                
            }

            return this.result;
        }

    }
}
