using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Signum.Entities;
using Signum.Engine;

namespace Signum.Web
{
    public class LiteJavaScriptConverter : JavaScriptConverter
    {
        public override IEnumerable<Type> SupportedTypes
        {
            get { return new[] { typeof(Lite) }; }
        }

        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            if (type == typeof(Lite))
            {
                string liteKey = (string)dictionary["Key"];
                return TypeLogic.ParseLite(typeof(Lite), liteKey);
            }

            return null;
        }

        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            var result = new Dictionary<string, object>();
            
            Lite lite = obj as Lite;
            if (lite != null)
            {
                result["Key"] = lite.Key();
                result["Id"] = lite.Id;
                result["ToStr"] = lite.ToString();
            }

            return result;
        }
    }
}
