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
    public class MListJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IMListPrivate).IsAssignableFrom(objectType);
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            giWriteJsonInternal.GetInvoker(value.GetType().ElementType())(writer, value, serializer);
        }

        static GenericInvoker<Action<JsonWriter, object, JsonSerializer>> giWriteJsonInternal = new GenericInvoker<Action<JsonWriter, object, JsonSerializer>>(
            (writer, value, serializer) => WriteJsonInternal<int>(writer, (MList<int>)value, serializer));

        static void WriteJsonInternal<T>(JsonWriter writer, MList<T> value, JsonSerializer serializer)
        {
            writer.WriteStartArray();

            foreach (var item in ((IMListPrivate<T>)value).InnerList)
            {
                writer.WriteStartObject();
                if (item.RowId != null)
                {
                    writer.WritePropertyName("rowId");
                    serializer.Serialize(writer, item.RowId);
                }

                if (item.OldIndex != null)
                {
                    writer.WritePropertyName("oldIndex");
                    serializer.Serialize(writer, item.OldIndex);
                }

                writer.WritePropertyName("element");
                serializer.Serialize(writer, item.Element);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Assert(reader, JsonToken.StartArray);

            throw new NotImplementedException();
        }

        private static void Assert(JsonReader reader, JsonToken expected)
        {
            if (reader.TokenType != expected)
                throw new InvalidOperationException("expected {0} but {1} found".FormatWith(expected, reader.TokenType));
        }
    }


}