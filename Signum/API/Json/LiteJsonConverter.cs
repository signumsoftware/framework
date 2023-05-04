using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.API.Json;

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

        if (lite.ModelType != Lite.DefaultModelType(lite.EntityType))
            writer.WriteString("ModelType", Lite.ModelTypeToString(lite.ModelType));

        writer.WritePropertyName("id");
        JsonSerializer.Serialize(writer, lite.IdOrNull?.Object, lite.IdOrNull?.Object.GetType() ?? typeof(object), options);

        if (lite.Model != null)
        {
            if (lite.Model is string str)
                writer.WriteString("model", str);
            else
            {
                writer.WritePropertyName("model");

                var pr = PropertyRoute.Root(lite.Model.GetType());
                var model = (ModelEntity)lite.Model;
                using (EntityJsonContext.SetCurrentPropertyRouteAndEntity((pr, model, null)))
                    JsonSerializer.Serialize(writer, model, options);
            }
        }

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

    public override Lite<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, Lite<T>? existingValue, Func<string, Type>? parseType)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        reader.Assert(JsonTokenType.StartObject);

        object? model = null;
        string? idObj = null;
        Type? type = null;
        string? modelTypeStr = null;
        Entity? entity = null;

        reader.Read();
        while (reader.TokenType == JsonTokenType.PropertyName)
        {
            var propName = reader.GetString();
            switch (propName)
            {
                case "EntityType": reader.Read(); type = TypeLogic.GetType(reader.GetString()!); break;
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
                case "ModelType": reader.Read(); modelTypeStr = reader.GetString(); break;
                case "model":
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.String)
                        model = reader.GetString();
                    else
                    {
                        using (EntityJsonConverterFactory.SetPath(".model"))
                        {
                            var converter = (JsonConverterWithExisting<ModelEntity>)options.GetConverter(typeof(ModelEntity));
                            model = converter.Read(ref reader, typeof(ModelEntity), options, (ModelEntity?)existingValue?.Model, modelTypeStr => Lite.ParseModelType(type!, modelTypeStr));
                        }
                    }
                    break;
                case "entity":
                    reader.Read();
                    using (EntityJsonConverterFactory.SetPath(".entity"))
                    {
                        var converter = (JsonConverterWithExisting<Entity>)options.GetConverter(typeof(Entity));
                        entity = converter.Read(ref reader, typeof(Entity), options, (Entity?)(IEntity?)existingValue?.EntityOrNull, null);
                    }
                    break;
                default: throw new JsonException("unexpected property " + propName);
            }

            reader.Read();
        }

        reader.Assert(JsonTokenType.EndObject);

        PrimaryKey? idOrNull = idObj == null ? (PrimaryKey?)null : PrimaryKey.Parse(idObj, type!);


        Type getModelType()
        {
            return modelTypeStr == null ? Lite.DefaultModelType(type!) : Lite.ParseModelType(type!, modelTypeStr);
        }

        if (entity == null)
        {
            return model != null ?
                (Lite<T>)Lite.Create(type!, idOrNull!.Value, model) :
                (Lite<T>)Lite.Create(type!, idOrNull!.Value, getModelType());
        }

        var result = model != null ?
            (Lite<T>)entity.ToLiteFat(model) :
            (Lite<T>)entity.ToLiteFat(getModelType()); ;

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


