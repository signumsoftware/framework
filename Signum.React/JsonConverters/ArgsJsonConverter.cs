using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.React.Facades;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Signum.React.Json
{
    public class ArgsJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(object[]);
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            List<object> args = new List<object>();

            reader.Assert(JsonToken.StartArray);

            reader.Read();


            while (reader.TokenType != JsonToken.EndArray)
            {
                var token = JToken.Load(reader);

                var converted = ConvertObject(token, serializer);

                args.Add(converted);

                reader.Read();
            }

            reader.Assert(JsonToken.EndArray);

            return args.ToArray();
        }

        private object ConvertObject(JToken token, JsonSerializer serializer)
        {
            if (token == null)
                return null;
            if (token is JValue)
            {
                var obj = ((JValue) token).Value;
                return obj;
            }

            if (token is JObject)
            {
                var j = (JObject)token;

                if (j.Property("EntityType") != null)
                    return serializer.Deserialize(new JTokenReader(j), typeof(Lite<Entity>));

                if (j.Property("Type") != null)
                    return serializer.Deserialize(new JTokenReader(j), typeof(ModifiableEntity));
            }
            else if (token is JArray)
            {
                var a = (JArray)token;
                var result = a.Select(t => ConvertObject(t, serializer)).ToList();
                return result;

            }

            throw new NotSupportedException("Unable to deserialize dynamically:" + token.ToString());
        }

    }
}