using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Signum.Entities;
using Signum.Engine;
using Signum.Utilities;

namespace Signum.Web
{
    public class LiteJavaScriptConverter : JavaScriptConverter
    {
        public override IEnumerable<Type> SupportedTypes
        {
            get { return new[] { typeof(LiteImp) }; }
        }

        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            if (type == typeof(Lite<IdentifiableEntity>))
            {
                string liteKey = (string)dictionary["Key"];
                return Lite.Parse(liteKey);
            }

            return null;
        }

        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            var result = new Dictionary<string, object>();

            Lite<IdentifiableEntity> lite = obj as Lite<IdentifiableEntity>;
            if (lite != null)
            {
                result["Key"] = lite.Key();
                result["Id"] = lite.Id;
                result["ToStr"] = lite.ToString();
            }

            return result;
        }
    }

    public class EnumJavaScriptConverter : JavaScriptConverter
    {
        public override IEnumerable<Type> SupportedTypes
        {
            get { return new[] { typeof(Enum) }; }
        }

        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            var result = new Dictionary<string, object>();

            Enum myEnum = obj as Enum;
            if (myEnum != null)
            {
                result["Id"] = Convert.ToInt32(myEnum);
                result["Value"] = myEnum.ToString();
                result["ToStr"] =  myEnum.NiceToString();
            }

            return result;
        }
    }
}
