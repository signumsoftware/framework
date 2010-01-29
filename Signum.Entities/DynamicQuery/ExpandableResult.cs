using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.DynamicQuery
{
    public abstract class Expandable
    {
        public object[] Expansions { get; set; }
    }

    public class Expandable<T> : Expandable
    {
        public Expandable(T value)
        {
            this.Value = value;
        }

        public T Value { get; private set;}
    }
}
