using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Signum.Engine;
using Signum.Entities;
using Signum.React.ApiControllers;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.React.Json
{
    public class ArgsJsonConverter : JsonConverter
    {
        public static Dictionary<OperationSymbol, Func<JToken, object>> CustomOperationArgsConverters =
            new Dictionary<OperationSymbol, Func<JToken, object>>();

        public static void RegisterCustomOperationArgsConverter(OperationSymbol operationSymbol, Func<JToken, object> converter)
        {
            CustomOperationArgsConverters[operationSymbol] =
                CustomOperationArgsConverters.TryGetC(operationSymbol) +
                converter;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(OperationController.BaseOperationRequest).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var request = (OperationController.BaseOperationRequest)Activator.CreateInstance(objectType);
            serializer.Populate(reader, request);
            var operationSymbol = SymbolLogic<OperationSymbol>.ToSymbol(request.operationKey);
            if (request.args != null)
                for (int i = 0; i < request.args.Length; i++)
                {
                    if (request.args[i] is JToken jtoken)
                        request.args[i] = ConvertObject(jtoken, serializer, operationSymbol);
                }

            return request;
        }

        private object ConvertObject(JToken token, JsonSerializer serializer, OperationSymbol operationSymbol)
        {
            if (token == null)
                return null;

            if (token is JValue)
            {
                var obj = ((JValue)token).Value;
                return obj;
            }

            if (token is JObject j)
            {
                if (j.Property("EntityType") != null)
                    return serializer.Deserialize(new JTokenReader(j), typeof(Lite<Entity>));

                if (j.Property("Type") != null)
                    return serializer.Deserialize(new JTokenReader(j), typeof(ModifiableEntity));
            }
            else if (token is JArray a)
            {
                var result = a.Select(t => ConvertObject(t, serializer, operationSymbol)).ToList();
                return result;

            }

            var conv = CustomOperationArgsConverters.TryGetC(operationSymbol);

            if (conv == null)
                throw new InvalidOperationException("Impossible to deserialize request before executing {0}.\r\nConsider registering your own converter in 'CustomOperationArgsConverters'.\r\nReceived JSON:\r\n\r\n{1}".FormatWith(operationSymbol, token));

            return conv.GetInvocationListTyped().Select(f => conv(token)).NotNull().FirstOrDefault();
        }
    }
}