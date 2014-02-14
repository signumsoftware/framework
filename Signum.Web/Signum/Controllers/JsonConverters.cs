using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Signum.Entities;
using Signum.Engine;
using Signum.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Signum.Web.Controllers
{
    public class LiteJavaScriptConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Lite<IdentifiableEntity>).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);

            string liteKey = (string)jsonObject["Key"];
            return Lite.Parse(liteKey);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Lite<IdentifiableEntity> lite = (Lite<IdentifiableEntity>)value;
            writer.WriteStartObject();

            writer.WritePropertyName("Key");
            serializer.Serialize(writer, lite.Key());
            writer.WritePropertyName("Id");
            serializer.Serialize(writer, lite.Id);
            writer.WritePropertyName("ToStr");
            serializer.Serialize(writer, lite.ToString());

            writer.WriteEndObject();
        }
    }

    public class EnumJavaScriptConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsEnum;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Enum myEnum = (Enum)value;
            writer.WriteStartObject();

            writer.WritePropertyName("Id");
            serializer.Serialize(writer, Convert.ToInt32(myEnum));
            writer.WritePropertyName("Value");
            serializer.Serialize(writer, myEnum.ToString());
            writer.WritePropertyName("ToStr");
            serializer.Serialize(writer, myEnum.NiceToString());

            writer.WriteEndObject();
        }
    }
}
