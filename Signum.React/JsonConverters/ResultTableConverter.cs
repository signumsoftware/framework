using Newtonsoft.Json;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.React.ApiControllers;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Signum.React.Json
{
    public class ResultTableConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ResultTable).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            using (HeavyProfiler.LogNoStackTrace("ReadJson", () => typeof(ResultTable).Name))
            {
                var rt = (ResultTable)value;

                writer.WriteStartObject();

                writer.WritePropertyName("entityColumn");
                writer.WriteValue(rt.EntityColumn?.Name);

                writer.WritePropertyName("columns");
                serializer.Serialize(writer, rt.Columns.Select(c => c.Column.Token.FullKey()).ToList());

                writer.WritePropertyName("pagination");
                serializer.Serialize(writer, new PaginationTS(rt.Pagination));

                writer.WritePropertyName("totalElements");
                writer.WriteValue(rt.TotalElements);


                writer.WritePropertyName("rows");
                writer.WriteStartArray();
                foreach (var row in rt.Rows)
                {
                    writer.WriteStartObject();
                    if (rt.EntityColumn != null)
                    {
                        writer.WritePropertyName("entity");
                        serializer.Serialize(writer, row.Entity);
                    }

                    writer.WritePropertyName("columns");
                    writer.WriteStartArray();
                    foreach (var column in rt.Columns)
                    {
                        using (JsonSerializerExtensions.SetCurrentPropertyRoute(column.Column.Token.GetPropertyRoute()))
                        {
                            serializer.Serialize(writer, row[column]);
                        }
                    }
                    writer.WriteEndArray();


                    writer.WriteEndObject();

                }
                writer.WriteEndArray();


                writer.WriteEndObject();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get { return false; }
        }
    }


}