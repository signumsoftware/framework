using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Signum.Web
{
    public static class DictionaryExtensions
    {
        public static void AddRangeFromAnonymousType(this Dictionary<string, object> dictionary, object values)
        {
            if (values != null)
            {
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(values))
                {
                    object obj2 = descriptor.GetValue(values);
                    dictionary.Add(descriptor.Name, obj2);
                }
            }

        }
    }
}
