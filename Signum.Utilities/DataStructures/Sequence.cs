using System.Collections.Generic;

namespace Signum.Utilities.DataStructures
{
    public class Sequence<T> : List<T>
    {
        public void Add(IEnumerable<T> collection)
        {
            if (collection != null)
            {
                AddRange(collection);
            }
        }
    }
}
