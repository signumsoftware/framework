using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Signum.Engine;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
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
            var obj = JObject.Load(reader);

            if (obj.Property("operation") != null)
                return new FilterConditionTS
                {
                    token = obj.Property("token").Value.Value<string>(),
                    operation = obj.Property("operation").Value.ToObject<FilterOperation>(),
                    value = obj.Property("value")?.Value,
                };

            if (obj.Property("groupOperation") != null)
                return new FilterGroupTS
                {
                    groupOperation = obj.Property("groupOperation").Value.ToObject<FilterGroupOperation>(),
                    token = obj.Property("token")?.Value.Value<string>(),
                    filters = obj.Property("filters").Value.Select(a => a.ToObject<FilterTS>()).ToList()
                };

            throw new InvalidOperationException("Impossible to determine type of filter");
        }
    }
}