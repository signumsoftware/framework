using Newtonsoft.Json;
using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Signum.React.Json
{
    public static class JsonSerializerExtensions
    {
        public static object DeserializeValue(this JsonSerializer serializer, JsonReader reader, Type valueType, object oldValue)
        {
            if (oldValue != null)
            {
                var conv = serializer.Converters.FirstOrDefault(c => c.CanConvert(valueType));

                if (conv != null)
                    return conv.ReadJson(reader, valueType, oldValue, serializer);
            }

            return serializer.Deserialize(reader, valueType);
        }

        public static void Assert(this JsonReader reader, JsonToken expected)
        {
            if (reader.TokenType != expected)
                throw new JsonSerializationException($"Expected '{expected}' but '{reader.TokenType}' found in '{reader.Path}'");
        }


        static readonly ThreadVariable<PropertyRoute> currentPropertyRoute = Statics.ThreadVariable<PropertyRoute>("jsonPropertyRoute");

        public static PropertyRoute CurrentPropertyRoute
        {
            get { return currentPropertyRoute.Value; }
        }

        public static IDisposable SetCurrentPropertyRoute(PropertyRoute route)
        {
            var old = currentPropertyRoute.Value;

            currentPropertyRoute.Value = route;

            return new Disposable(() => { currentPropertyRoute.Value = old; });
        }
    }
}