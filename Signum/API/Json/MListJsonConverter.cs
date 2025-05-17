using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;

namespace Signum.API.Json;

interface IJsonConverterWithExisting
{
    object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, object? existingValue, Func<string, Type>? parseType);
}

public abstract class JsonConverterWithExisting<T> : JsonConverter<T>, IJsonConverterWithExisting
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Read(ref reader, typeToConvert, options, default, null);

    public object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, object? existingValue, Func<string, Type>? parseType) =>
        Read(ref reader, typeToConvert, options, (T?)existingValue, parseType);

    public abstract T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, T? existingValue, Func<string, Type>? parseType);
}

public class MListJsonConverterFactory : JsonConverterFactory
{
    protected readonly Action<PropertyRoute, ModifiableEntity?, SerializationMetadata?> AsserCanWrite;

    public MListJsonConverterFactory(Action<PropertyRoute, ModifiableEntity?, SerializationMetadata?> asserCanWrite)
    {
        AsserCanWrite = asserCanWrite;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(IMListPrivate).IsAssignableFrom(typeToConvert);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(typeof(MListJsonConverter<>).MakeGenericType(typeToConvert.ElementType()!), options, AsserCanWrite)!;
    }
}

public class MListJsonConverter<T> : JsonConverterWithExisting<MList<T>>
{
    protected readonly Action<PropertyRoute, ModifiableEntity?, SerializationMetadata?> AsserCanWrite;

    protected virtual bool IsAllowedToAddNewValue(T newValue)
    {
        return true;
    }

    protected virtual object? OnGetRowIdValue(ref Utf8JsonReader reader)
    {
        return reader.GetLiteralValue();
    }

    protected readonly JsonConverter<T> converter;
    public MListJsonConverter(JsonSerializerOptions options, Action<PropertyRoute, ModifiableEntity?, SerializationMetadata?> asserCanWrite)
    {
        this.converter = (JsonConverter<T>)options.GetConverter(typeof(T));
        AsserCanWrite = asserCanWrite;
    }

    public override void Write(Utf8JsonWriter writer, MList<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        var step = EntityJsonContext.CurrentSerializationPath!.Peek();

        var elementPr = step.Route.Add("Item");

        foreach (var item in ((IMListPrivate<T>)value).InnerList)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("rowId");
            JsonSerializer.Serialize(writer, item.RowId?.Object, item.RowId?.Object.GetType() ?? typeof(object), options);

            writer.WritePropertyName("element");
            using (EntityJsonContext.AddSerializationStep(new (elementPr, item.Element as ModifiableEntity, rowId: item.RowId)))
            {
                JsonSerializer.Serialize(writer, item.Element, item.Element?.GetType() ?? typeof(T), options);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public override MList<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, MList<T>? existingValue, Func<string, Type>? parseType)
    {

        var existingMList = (IMListPrivate<T>?)existingValue;

        var dic = existingMList == null ? new Dictionary<PrimaryKey, MList<T>.RowIdElement>() :
             existingMList.InnerList.Where(a => a.RowId.HasValue).ToDictionary(a => a.RowId!.Value, a => a);

        var newList = new List<MList<T>.RowIdElement>();

        var path = EntityJsonContext.CurrentSerializationPath!;

        var pr = path.CurrentPropertyRoute();
        var elementPr = pr!.Add("Item");

        var rowIdType = GetRowIdTypeFromAttribute(pr);

        reader.Assert(JsonTokenType.StartArray);

        reader.Read();

        while (reader.TokenType == JsonTokenType.StartObject)
        {
            reader.Read();
            reader.Assert(JsonTokenType.PropertyName);
            if (reader.GetString() != "rowId")
                throw new JsonException($"member 'rowId' expected in {reader.CurrentState}");

            reader.Read();
            var rowIdValue = OnGetRowIdValue(ref reader);

            reader.Read();
            reader.Assert(JsonTokenType.PropertyName);
            if (reader.GetString() != "element")
                throw new JsonException($"member 'element' expected in {reader.CurrentState}");

            reader.Read();

            using (EntityJsonConverterFactory.SetPath($"[{newList.Count}].element"))
            {
                if (rowIdValue != null && !rowIdValue.Equals(GraphExplorer.DummyRowId.Object))
                {
                    var rowId = new PrimaryKey((IComparable)ReflectionTools.ChangeType(rowIdValue, rowIdType)!);

                    var oldValue = dic.TryGetS(rowId);

                    using (EntityJsonContext.AddSerializationStep(new (elementPr, oldValue == null ? null : oldValue.Value.Element as ModifiableEntity, rowId)))
                    {
                        if (oldValue == null)
                        {
                            T newValue = (T)converter.Read(ref reader, typeof(T), options)!;

                            if (IsAllowedToAddNewValue(newValue))
                                newList.Add(new MList<T>.RowIdElement(newValue, rowId, null));
                        }
                        else
                        {
                            T newValue = converter is JsonConverterWithExisting<T> jcwe ?
                                (T)jcwe.Read(ref reader, typeof(T), options, oldValue.Value.Element!, null)! :
                                (T)converter.Read(ref reader, typeof(T), options)!;

                            if (IsAllowedToAddNewValue(newValue))
                            {
                                if (oldValue.Value.Element!.Equals(newValue))
                                    newList.Add(new MList<T>.RowIdElement(newValue, rowId, oldValue.Value.OldIndex));
                                else
                                    newList.Add(new MList<T>.RowIdElement(newValue));
                            }
                        }
                    }
                }
                else
                {
                    using (EntityJsonContext.AddSerializationStep(new (elementPr, null, (PrimaryKey?)null)))
                    {
                        var newValue = (T)converter.Read(ref reader, typeof(T), options)!;

                        if (IsAllowedToAddNewValue(newValue))
                            newList.Add(new MList<T>.RowIdElement(newValue));
                    }
                }
            }

            reader.Read();
            reader.Assert(JsonTokenType.EndObject);
            reader.Read();
        }

        reader.Assert(JsonTokenType.EndArray);

        if (existingMList == null) //Strange case...
        {
            if (newList.IsEmpty())
                return null!;
            else
                existingMList = new MList<T>();
        }

        bool orderMatters = GetPreserveOrderFromAttribute(pr);

        if (!existingMList.IsEqualTo(newList, orderMatters))
        {
            if (!EntityJsonContext.AllowDirectMListChanges)
                return new MList<T>(newList);

            var mod = path.CurrentModifiableEntity();
            var metadata = path.CurrentSerializationMetadata();

            this.AsserCanWrite(pr, mod, metadata);

            existingMList.AssignMList(newList);
        }

        return (MList<T>)existingMList;
    }

    protected static Type GetRowIdTypeFromAttribute(PropertyRoute route)
    {
        var settings = Schema.Current.Settings;
        var att = settings.FieldAttribute<PrimaryKeyAttribute>(route) ??
            (route.IsVirtualMList() ? settings.TypeAttribute<PrimaryKeyAttribute>(route.Type.ElementType()!) : null) ??
            settings.DefaultPrimaryKeyAttribute;

        return att.Type;
    }

    protected static bool GetPreserveOrderFromAttribute(PropertyRoute route)
    {
        var att = Schema.Current.Settings.FieldAttribute<PreserveOrderAttribute>(route);

        return att != null;
    }
}


