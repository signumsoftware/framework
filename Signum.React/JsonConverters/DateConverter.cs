using Newtonsoft.Json;
using Signum.Utilities;
using System;
using System.Globalization;

namespace Signum.React.Json
{
    public class DateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Date).IsAssignableFrom(objectType.UnNullify());
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer.WriteValue(((DateTime?)(Date?)value)?.ToString("o"));
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var date = reader.Value as DateTime?;
            return  (Date?)date;
        }
    }
}
