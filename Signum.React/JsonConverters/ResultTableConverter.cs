using Signum.Engine.Json;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.React.ApiControllers;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.React.Json
{
    public class ResultTableConverter : JsonConverter<ResultTable>
    {
        public override void Write(Utf8JsonWriter writer, ResultTable value, JsonSerializerOptions options)
        {
            using (HeavyProfiler.LogNoStackTrace("ReadJson", () => typeof(ResultTable).Name))
            {
                var rt = (ResultTable)value!;

                writer.WriteStartObject();

                writer.WritePropertyName("entityColumn");
                writer.WriteStringValue(rt.EntityColumn?.Name);

                writer.WritePropertyName("columns");
                JsonSerializer.Serialize(writer, rt.Columns.Select(c => c.Column.Token.FullKey()).ToList(), typeof(List<string>), options);

                writer.WritePropertyName("pagination");
                JsonSerializer.Serialize(writer, new PaginationTS(rt.Pagination), typeof(PaginationTS), options);

                writer.WritePropertyName("totalElements");
                if (rt.TotalElements == null)
                    writer.WriteNullValue();
                else
                    writer.WriteNumberValue(rt.TotalElements!.Value);


                writer.WritePropertyName("rows");
                writer.WriteStartArray();
                foreach (var row in rt.Rows)
                {
                    writer.WriteStartObject();
                    if (rt.EntityColumn != null)
                    {
                        writer.WritePropertyName("entity");
                        JsonSerializer.Serialize(writer, row.Entity, options);
                    }

                    writer.WritePropertyName("columns");
                    writer.WriteStartArray();
                    foreach (var column in rt.Columns)
                    {
                        using (EntityJsonContext.SetCurrentPropertyRouteAndEntity((column.Column.Token.GetPropertyRoute()!, null, null)))
                        {
                            JsonSerializer.Serialize(writer, row[column], options);
                        }
                    }
                    writer.WriteEndArray();


                    writer.WriteEndObject();

                }
                writer.WriteEndArray();


                writer.WriteEndObject();
            }
        }

        public override ResultTable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }


}
