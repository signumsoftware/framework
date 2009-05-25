using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Reflection;

namespace Signum.Engine.Linq
{
    [DebuggerDisplay("Key =  {Key}  Count = {Count}")]
    [DebuggerTypeProxy(typeof(Proxy))]
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
        public object Key;
        public ArrayList List;

        public Proxy(IList bla)
        {
            List = new ArrayList(bla);
            PropertyInfo pi = bla.GetType().GetProperty("Key");
            Key = pi.GetValue(bla, null);
        }
    }
}
