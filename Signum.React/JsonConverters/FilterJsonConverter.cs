using Signum.Engine.Json;
using Signum.Entities.DynamicQuery;
using Signum.React.ApiControllers;
using Signum.Utilities;
using System;
using System.Buffers;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.React.Json
{
    public class FilterJsonConverter : JsonConverter<FilterTS>
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(FilterTS).IsAssignableFrom(objectType);
        }

        public override void Write(Utf8JsonWriter writer, FilterTS value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override FilterTS? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var doc = JsonDocument.ParseValue(ref reader))
            {
                var elem = doc.RootElement;

                if (elem.TryGetProperty("operation", out var oper))
                {
                    return new FilterConditionTS
                    {
                        token = elem.GetProperty("token").GetString()!,
                        operation = oper.GetString()!.ToEnum<FilterOperation>(),
                        value = elem.TryGetProperty("value", out var val) ? val.ToObject<object>(options) : null,
                    };
                }  

                if (elem.TryGetProperty("groupOperation", out var groupOper))
                    return new FilterGroupTS
                    {
                        groupOperation = groupOper.GetString()!.ToEnum<FilterGroupOperation>(),
                        token = elem.TryGetProperty("token", out var token)? token.GetString() : null,
                        filters = elem.GetProperty("filters").EnumerateArray().Select(a => a.ToObject<FilterTS>()!).ToList()
                    };

                throw new InvalidOperationException("Impossible to determine type of filter");
            }
        }
    }

   
}
