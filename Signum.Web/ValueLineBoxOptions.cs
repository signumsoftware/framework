using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using System.Reflection;
using Newtonsoft.Json;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Signum.Web
{
    [JsonConverter(typeof(ValueLineBoxOptionsConverter))]
    public class ValueLineBoxOptions
    {
        public string prefix;
        public ValueLineType type;
        public string title = SelectorMessage.ChooseAValue.NiceToString();
        public string message = SelectorMessage.PleaseChooseAValueToContinue.NiceToString();
        public string labelText = null;
        public string format = null;
        public string unit = null;

      
        public object value;

        public ValueLineBoxOptions(ValueLineType type, string prefix)
        {
            this.type = type;
            this.prefix = prefix;
        }

        public ValueLineBoxOptions(ValueLineType type, string parentPrefix, string newPart)
            :this(type, "_".CombineIfNotEmpty(parentPrefix, newPart))
        {
        }
    }

    class ValueLineBoxOptionsConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ValueLineBoxOptions);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ValueLineBoxOptions options = (ValueLineBoxOptions)value;

            writer.WriteStartObject();

            writer.WritePropertyName("prefix");
            writer.WriteValue(options.prefix);

            writer.WritePropertyName("type");
            writer.WriteValue(options.type);

            if (options.message != null)
            {
                writer.WritePropertyName("message");
                writer.WriteValue(options.message);
            }

            if (options.title != null)
            {
                writer.WritePropertyName("title");
                writer.WriteValue(options.title);
            }

            if (options.labelText != null)
            {
                writer.WritePropertyName("labelText");
                writer.WriteValue(options.labelText);
            }

            if (options.format != null)
            {
                writer.WritePropertyName("format");
                writer.WriteValue(options.format);
            }

            if (options.unit != null)
            {
                writer.WritePropertyName("unit");
                writer.WriteValue(options.unit);
            }
            
            if (options.value != null)
            {
                writer.WritePropertyName("value");
                writer.WriteValue(
                    options.value is IFormattable ? ((IFormattable)options.value).ToString(options.format, CultureInfo.CurrentCulture) :
                    options.value.ToString());
            }

            writer.WriteEndObject();
        }
    }

    
}
