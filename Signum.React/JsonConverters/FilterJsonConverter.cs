using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Signum.Engine;
using Signum.Entities;
using Signum.React.ApiControllers;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.React.Json
{
    public class FilterJsonConverter : JsonConverter
    { 
        public override bool CanConvert(Type objectType)
        {
            return typeof(FilterTS).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = (JObject)serializer.Deserialize(reader);

            if (obj.Property("operation") != null)
                return obj.ToObject<FilterConditionTS>();

            if (obj.Property("groupOperation") == null)
                return obj.ToObject<FilterGroupTS>();

            throw new InvalidOperationException("Impossible to determine type of filter");
        }
    }
}