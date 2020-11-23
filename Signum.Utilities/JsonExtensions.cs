using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Signum.Utilities
{
    public static class JsonExtensions
    {
        public static T ToObject<T>(this JsonElement element, JsonSerializerOptions? options = null)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(bufferWriter))
                element.WriteTo(writer);
            return JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan, options)!;
        }

        public static object ToObject(this JsonElement element, Type targetType, JsonSerializerOptions? options = null)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(bufferWriter))
                element.WriteTo(writer);
            return JsonSerializer.Deserialize(bufferWriter.WrittenSpan, targetType, options)!;
        }

        public static void Assert(this ref Utf8JsonReader reader, JsonTokenType expected)
        {
            if (reader.TokenType != expected)
                throw new JsonException($"Expected '{expected}' but '{reader.TokenType}' found in '{reader.CurrentState}'");
        }

        public static object? GetLiteralValue(this ref Utf8JsonReader reader)
        {
            return reader.TokenType == JsonTokenType.Null ? null! :
                   reader.TokenType == JsonTokenType.String ? reader.GetString() :
                   reader.TokenType == JsonTokenType.Number ? reader.GetInt64() :
                   reader.TokenType == JsonTokenType.True ? true :
                   reader.TokenType == JsonTokenType.False ? false :
                   throw new UnexpectedValueException(reader.TokenType);
        }

        //Binary
        public static string ToJsonString(object obj, JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Serialize(obj, obj.GetType(), options);
        }

        public static byte[] ToJsonBytes(object obj, JsonSerializerOptions? options = null)
        {
            using (MemoryStream ms = new MemoryStream())
            using (Utf8JsonWriter writer = new Utf8JsonWriter(ms))
            {
                JsonSerializer.Serialize(writer, obj, obj.GetType(), options);
                return ms.ToArray();
            }
        }

        public static void ToJsonFile(object graph, string fileName, JsonSerializerOptions? options = null)
        {
            using (FileStream fs = File.OpenWrite(fileName))
            using (Utf8JsonWriter writer = new Utf8JsonWriter(fs))
            {
                JsonSerializer.Serialize(writer, graph, graph.GetType(), options);
            }
        }

        public static T FromJsonBytes<T>(byte[] bytes, JsonSerializerOptions? options = null)
        {
            Utf8JsonReader reader = new Utf8JsonReader(bytes);
            var result = JsonSerializer.Deserialize<T>(ref reader, options);
            return result!;
        }

        public static T FromJsonFile<T>(string fileName, JsonSerializerOptions? options = null)
        {
            var bytes = File.ReadAllBytes(fileName);
            Utf8JsonReader reader = new Utf8JsonReader(bytes);
            var result = JsonSerializer.Deserialize<T>(ref reader, options);
            return result!;
        }

        public static T FromJsonString<T>(string json, JsonSerializerOptions? options = null)
        {
            var result = JsonSerializer.Deserialize<T>(json, options);
            return result!;
        }

    }
}
