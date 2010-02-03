using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.DynamicQuery
{
    public class Expandable<T>
    {
        public Expandable(T value, params object[] expansions)
        {
            this.Value = value;
            this.Expansions = expansions;
        }

        public object[] Expansions { get; private set; }
        public T Value { get; private set;}
    }
}
