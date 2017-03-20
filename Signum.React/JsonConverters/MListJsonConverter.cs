using Newtonsoft.Json;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Signum.React.Json
{
    public class MListJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IMListPrivate).IsAssignableFrom(objectType);
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            giWriteJsonInternal.GetInvoker(value.GetType().ElementType())(writer, (IMListPrivate)value, serializer);
        }

        static GenericInvoker<Action<JsonWriter, IMListPrivate, JsonSerializer>> giWriteJsonInternal = new GenericInvoker<Action<JsonWriter, IMListPrivate, JsonSerializer>>(
            (writer, value, serializer) => WriteJsonInternal<int>(writer, (MList<int>)value, serializer));

        static void WriteJsonInternal<T>(JsonWriter writer, MList<T> value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            var pr = JsonSerializerExtensions.CurrentPropertyRoute;

            var elementPr = pr.Add("Item");

            using (JsonSerializerExtensions.SetCurrentPropertyRoute(elementPr))
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

            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return giReadJsonInternal.GetInvoker(objectType.ElementType())(reader, (IMListPrivate)existingValue, serializer);
        }

        static GenericInvoker<Func<JsonReader, IMListPrivate, JsonSerializer, IMListPrivate>> giReadJsonInternal =
            new GenericInvoker<Func<JsonReader, IMListPrivate, JsonSerializer, IMListPrivate>>(
                (reader, value, serializer) => ReadJsonInternal<int>(reader, (IMListPrivate<int>)value, serializer));

        static MList<T> ReadJsonInternal<T>(JsonReader reader, IMListPrivate<T> existingValue, JsonSerializer serializer)
        {
            var dic = existingValue == null ? new Dictionary<PrimaryKey, MList<T>.RowIdElement>() :
                 existingValue.InnerList.Where(a => a.RowId.HasValue).ToDictionary(a => a.RowId.Value, a => a);

            var newList = new List<MList<T>.RowIdElement>();

            var pr = JsonSerializerExtensions.CurrentPropertyRoute;

            var elementPr = pr.Add("Item");

            var rowIdType = GetRowIdTypeFromAttribute(pr);
               

            reader.Assert(JsonToken.StartArray);

            reader.Read();

            using (JsonSerializerExtensions.SetCurrentPropertyRoute(elementPr))
            {
                while (reader.TokenType == JsonToken.StartObject)
                {
                    reader.Read();
                    reader.Assert(JsonToken.PropertyName);
                    if (((string)reader.Value) != "rowId")
                        throw new JsonSerializationException($"member 'rowId' expected in {reader.Path}");

                    reader.Read();
                    var rowIdValue = reader.Value;

                    reader.Read();
                    reader.Assert(JsonToken.PropertyName);
                    if (((string)reader.Value) != "element")
                        throw new JsonSerializationException($"member 'element' expected in {reader.Path}");

                    reader.Read();
                    if (rowIdValue != null && !rowIdValue.Equals(GraphExplorer.DummyRowId.Object))
                    { 
                        var rowId = new PrimaryKey((IComparable)ReflectionTools.ChangeType(rowIdValue, rowIdType));
                        
                        var oldValue = dic.TryGetS(rowId);

                        if (oldValue == null)
                        {
                            T newValue = (T)serializer.DeserializeValue(reader, typeof(T), null);

                            newList.Add(new MList<T>.RowIdElement(newValue, rowId, null));
                        }
                        else
                        {
                            T newValue = (T)serializer.DeserializeValue(reader, typeof(T), oldValue.Value.Element);

                            if (oldValue.Value.Element.Equals(newValue))
                                newList.Add(new MList<T>.RowIdElement(newValue, rowId, oldValue.Value.OldIndex));
                            else
                                newList.Add(new MList<T>.RowIdElement(newValue));
                        }
                    }
                    else
                    {
                        var newValue = (T)serializer.DeserializeValue(reader, typeof(T), null);
                        newList.Add(new MList<T>.RowIdElement(newValue));
                    }

                    reader.Read();

                    reader.Assert(JsonToken.EndObject);
                    reader.Read();
                }
            }
            
            reader.Assert(JsonToken.EndArray);

            if (existingValue == null) //Strange case...
            {
                if (newList.IsEmpty())
                    return null;
                else
                    existingValue = new MList<T>();
            }

            bool orderMatters = GetPreserveOrderFromAttribute(pr);

            if (!existingValue.IsEqualTo(newList,orderMatters))
            {
                EntityJsonConverter.AssertCanWrite(pr);

                existingValue.AssignMList(newList);
            }

            return (MList<T>)existingValue;
        }

        private static Type GetRowIdTypeFromAttribute(PropertyRoute route)
        {
            var att = Schema.Current.Settings.FieldAttribute<PrimaryKeyAttribute>(route) ?? Schema.Current.Settings.DefaultPrimaryKeyAttribute;
            
            return att.Type;
        }

        private static bool GetPreserveOrderFromAttribute(PropertyRoute route)
        {
            var att = Schema.Current.Settings.FieldAttribute<PreserveOrderAttribute>(route);

            return att!=null;
        }
    }


}