using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.React.Json
{
    public class MListJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IMListPrivate).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            giWriteJsonInternal.GetInvoker(value!.GetType().ElementType()!)(writer, (IMListPrivate)value, serializer);
        }

        static GenericInvoker<Action<JsonWriter, IMListPrivate, JsonSerializer>> giWriteJsonInternal = new GenericInvoker<Action<JsonWriter, IMListPrivate, JsonSerializer>>(
            (writer, value, serializer) => WriteJsonInternal<int>(writer, (MList<int>)value, serializer));

        static void WriteJsonInternal<T>(JsonWriter writer, MList<T> value, JsonSerializer serializer)
        {
            var errors = new List<string>();
            serializer.Error += delegate (object? sender, ErrorEventArgs args)
            {
                // only log an error once
                if (args.CurrentObject == args.ErrorContext.OriginalObject)
                {
                    errors.Add(args.ErrorContext.Error.Message);
                }
            };

            writer.WriteStartArray();
            var tup = JsonSerializerExtensions.CurrentPropertyRouteAndEntity!.Value;

            var elementPr = tup.pr.Add("Item");

            using (JsonSerializerExtensions.SetCurrentPropertyRouteAndEntity((elementPr, tup.mod)))
            {
                foreach (var item in ((IMListPrivate<T>)value).InnerList)
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("rowId");
                    writer.WriteValue(item.RowId?.Object);

                    writer.WritePropertyName("element");
                    serializer.Serialize(writer, item.Element);

                    writer.WriteEndObject();
                }
            }

            if (errors.Any())
                throw new JsonSerializationException(errors.ToString("\r\n"));

            writer.WriteEndArray();
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return giReadJsonInternal.GetInvoker(objectType.ElementType()!)(reader, (IMListPrivate)existingValue!, serializer);
        }

        static GenericInvoker<Func<JsonReader, IMListPrivate, JsonSerializer, IMListPrivate>> giReadJsonInternal =
            new GenericInvoker<Func<JsonReader, IMListPrivate, JsonSerializer, IMListPrivate>>(
                (reader, value, serializer) => ReadJsonInternal<int>(reader, (IMListPrivate<int>)value, serializer));

        static MList<T> ReadJsonInternal<T>(JsonReader reader, IMListPrivate<T> existingValue, JsonSerializer serializer)
        {
            var errors = new List<string>();
            serializer.Error += delegate (object? sender, ErrorEventArgs args)
            {
                // only log an error once
                if (args.CurrentObject == args.ErrorContext.OriginalObject)
                {
                    errors.Add(args.ErrorContext.Error.Message);
                }
            };

            var dic = existingValue == null ? new Dictionary<PrimaryKey, MList<T>.RowIdElement>() :
                 existingValue.InnerList.Where(a => a.RowId.HasValue).ToDictionary(a => a.RowId!.Value, a => a);

            var newList = new List<MList<T>.RowIdElement>();

            var tup = JsonSerializerExtensions.CurrentPropertyRouteAndEntity!.Value;

            var elementPr = tup.pr.Add("Item");

            var rowIdType = GetRowIdTypeFromAttribute(tup.pr);

            reader.Assert(JsonToken.StartArray);

            reader.Read();

            using (JsonSerializerExtensions.SetCurrentPropertyRouteAndEntity((elementPr, tup.mod)))
            {
                while (reader.TokenType == JsonToken.StartObject)
                {
                    reader.Read();
                    reader.Assert(JsonToken.PropertyName);
                    if (((string)reader.Value!) != "rowId")
                        throw new JsonSerializationException($"member 'rowId' expected in {reader.Path}");

                    reader.Read();
                    var rowIdValue = reader.Value;

                    reader.Read();
                    reader.Assert(JsonToken.PropertyName);
                    if (((string)reader.Value!) != "element")
                        throw new JsonSerializationException($"member 'element' expected in {reader.Path}");

                    reader.Read();
                    if (rowIdValue != null && !rowIdValue.Equals(GraphExplorer.DummyRowId.Object))
                    {
                        var rowId = new PrimaryKey((IComparable)ReflectionTools.ChangeType(rowIdValue, rowIdType)!);

                        var oldValue = dic.TryGetS(rowId);

                        if (oldValue == null)
                        {
                            T newValue = (T)serializer.DeserializeValue(reader, typeof(T), null)!;

                            newList.Add(new MList<T>.RowIdElement(newValue, rowId, null));
                        }
                        else
                        {
                            T newValue = (T)serializer.DeserializeValue(reader, typeof(T), oldValue.Value.Element)!;

                            if (oldValue.Value.Element!.Equals(newValue))
                                newList.Add(new MList<T>.RowIdElement(newValue, rowId, oldValue.Value.OldIndex));
                            else
                                newList.Add(new MList<T>.RowIdElement(newValue));
                        }
                    }
                    else
                    {
                        var newValue = (T)serializer.DeserializeValue(reader, typeof(T), null)!;
                        newList.Add(new MList<T>.RowIdElement(newValue));
                    }

                    if (errors.Any())
                        throw new JsonSerializationException(errors.ToString("\r\n"));

                    reader.Read();
                    reader.Assert(JsonToken.EndObject);
                    reader.Read();
                }
            }

            reader.Assert(JsonToken.EndArray);

            if (existingValue == null) //Strange case...
            {
                if (newList.IsEmpty())
                    return null!;
                else
                    existingValue = new MList<T>();
            }

            bool orderMatters = GetPreserveOrderFromAttribute(tup.pr);

            if (!existingValue.IsEqualTo(newList, orderMatters))
            {
                if (!JsonSerializerExtensions.AllowDirectMListChanges)
                    return new MList<T>(newList);

                EntityJsonConverter.AssertCanWrite(tup.pr, tup.mod);

                existingValue.AssignMList(newList);
            }

            return (MList<T>)existingValue;
        }

        private static Type GetRowIdTypeFromAttribute(PropertyRoute route)
        {
            var settings = Schema.Current.Settings;
            var att = settings.FieldAttribute<PrimaryKeyAttribute>(route) ??
                (route.IsVirtualMList() ? settings.TypeAttribute<PrimaryKeyAttribute>(route.Type.ElementType()!) : null) ??
                settings.DefaultPrimaryKeyAttribute;

            return att.Type;
        }

        private static bool GetPreserveOrderFromAttribute(PropertyRoute route)
        {
            var att = Schema.Current.Settings.FieldAttribute<PreserveOrderAttribute>(route);

            return att!=null;
        }
    }


}
