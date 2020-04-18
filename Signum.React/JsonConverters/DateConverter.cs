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
            writer.WriteValue(((Date?)value)?.ToString("o"));
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return null;

            var date = reader.Value as DateTime?;
            if(date != null)
                return (Date?)date;

            return Date.ParseExact((string)reader.Value, "o", CultureInfo.CurrentCulture);
        }
    }
}
