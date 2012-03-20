using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Utilities.DataStructures
{
    public class Sequence<T> : List<T>
    {
        public void Add(IEnumerable<T> collection)
        {
            AddRange(collection);
        }
    }
}
