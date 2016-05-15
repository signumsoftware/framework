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
                var obj = serializer.Deserialize(reader);

                var converted = ConvertObject(obj, serializer);

                args.Add(converted);

                reader.Read();
            }

            reader.Assert(JsonToken.EndArray);

            return args.ToArray();
        }

        private object ConvertObject(object obj, JsonSerializer serializer)
        {
            if (obj is string || obj is DateTime || obj is long || obj is double)
            {
                return obj;
            }
            else if (obj is JObject)
            {
                var j = (JObject)obj;

                if (j.Property("EntityType") != null)
                    return serializer.Deserialize(new JTokenReader(j), typeof(Lite<Entity>));

                if (j.Property("Type") != null)
                    return serializer.Deserialize(new JTokenReader(j), typeof(ModifiableEntity));
            }

            throw new NotSupportedException("Unable to deserialize dynamically:" + obj.ToString());
        }
    }
}