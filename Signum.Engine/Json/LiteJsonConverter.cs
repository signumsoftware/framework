using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Utilities;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.Engine.Json
{
    public class LiteJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(Lite<IEntity>).IsAssignableFrom(typeToConvert);
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter)Activator.CreateInstance(typeof(LiteJsonConverter<>).MakeGenericType(Lite.Extract(typeToConvert)!))!;
        }
    }


    public class LiteJsonConverter<T> : JsonConverterWithExisting<Lite<T>>
        where T : class, IEntity
    {
        public override void Write(Utf8JsonWriter writer, Lite<T> value, JsonSerializerOptions options)
        {
            var lite = value;
            writer.WriteStartObject();

            writer.WriteString("EntityType", TypeLogic.GetCleanName(lite.EntityType));

            writer.WritePropertyName("id");
            JsonSerializer.Serialize(writer, lite.IdOrNull?.Object, lite.IdOrNull?.Object.GetType() ?? typeof(object), options);

            writer.WriteString("toStr", lite.ToString());

            if (lite.EntityOrNull != null)
            {
                writer.WritePropertyName("entity");

                var pr = PropertyRoute.Root(lite.Entity.GetType());
                var entity = (ModifiableEntity)(IEntity)lite.EntityOrNull;
                using (EntityJsonContext.SetCurrentPropertyRouteAndEntity((pr, entity, null)))
                    JsonSerializer.Serialize(writer, lite.Entity, options);
            }

            writer.WriteEndObject();
        }

        public override Lite<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, Lite<T>? existingValue) 
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            reader.Assert(JsonTokenType.StartObject);

            string? toString = null;
            string? idObj = null;
            string? typeStr = null;
            Entity? entity = null;

            reader.Read();
            while (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propName = reader.GetString();
                switch (propName)
                {
                    case "toStr": reader.Read(); toString = reader.GetString(); break;
                    case "id":
                        {
                            reader.Read();
                            idObj =
                                reader.TokenType == JsonTokenType.Null ? null :
                                reader.TokenType == JsonTokenType.String ? reader.GetString() :
                                reader.TokenType == JsonTokenType.Number ? reader.GetInt64().ToString() :
                                reader.TokenType == JsonTokenType.True ? true.ToString() :
                                reader.TokenType == JsonTokenType.False ? false.ToString() :
                                throw new UnexpectedValueException(reader.TokenType);

                            break;
                        }
                    case "EntityType": reader.Read(); typeStr = reader.GetString(); break;
                    case "entity":
                        using (EntityJsonConverterFactory.SetPath(".entity"))
                        {
                            reader.Read();
                            var converter = (JsonConverterWithExisting<Entity>)options.GetConverter(typeof(Entity));
                            entity = converter.Read(ref reader, typeof(Entity), options, (Entity?)(IEntity?)existingValue?.EntityOrNull);
                        }
                        break;
                    default: throw new JsonException("unexpected property " + propName);
                }

                reader.Read();
            }

            reader.Assert(JsonTokenType.EndObject);

            Type type = TypeLogic.GetType(typeStr!);

            PrimaryKey? idOrNull = idObj == null ? (PrimaryKey?)null : PrimaryKey.Parse(idObj, type);

            if (entity == null)
                return (Lite<T>)Lite.Create(type, idOrNull!.Value, toString!);

            var result = (Lite<T>)entity.ToLiteFat(toString);

            if (result.EntityType != type)
                throw new InvalidOperationException("Types don't match");

            if (result.IdOrNull != idOrNull)
                throw new InvalidOperationException("Id's don't match");

            var existing = existingValue as Lite<T>;

            if (existing.Is(result) && existing!.EntityOrNull == null && result.EntityOrNull != null)
            {
                existing.SetEntity((Entity)(IEntity)result.EntityOrNull);
                return existing;
            }

           return result;
        }
    }


}
