using Newtonsoft.Json;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Signum.React.Json
{
    public class TimeSpanConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(TimeSpan).IsAssignableFrom(objectType.UnNullify());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((TimeSpan?) value)?.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var str = reader.Value as string;
            return string.IsNullOrEmpty(str) ? (TimeSpan?)null : TimeSpan.Parse(str);
        }
    }


}