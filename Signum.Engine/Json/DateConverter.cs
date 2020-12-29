using Signum.Utilities;
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.Engine.Json
{
    public class DateConverter : JsonConverter<Date>
    {
        public override Date Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Date.ParseExact((string)reader.GetString()!, "o", CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, Date value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("o", CultureInfo.InvariantCulture));
        }
    }
}
