using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Web
{
    public class JsOptionsBuilder : JsRenderer, IEnumerable<KeyValuePair<string, string>>
    {
        bool big;
        Dictionary<string, string> dic = new Dictionary<string, string>();

        public JsOptionsBuilder(bool big)
        {
            this.big = big;

            Renderer = () => "{{{0}}}".Formato(this.dic.ToString(kvp => "{0}: {1}".Formato(kvp.Key, kvp.Value), big ? ",\r\n" : ", "));
        }

        public void Add(string key, string value)
        {
            if (value == null)
                return;

            dic[key] = value;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return dic.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return dic.GetEnumerator();
        }

        public override string ToString()
        {
            return ToJS();
        }

        public string this[string key]
        {
            get { return dic.TryGetC(key); }
            set
            {
                if (dic == null)
                    dic.Remove(key);
                else
                    dic[key] = value;
            }
        }
    }
}
