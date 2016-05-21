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
                    writer.WriteValue(item.RowId == null ? null : item.RowId.Value.Object);

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

            var rowIdType = GetRowIdTypeFromSchema(pr);

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
                        if (rowIdType == null)
                            throw new InvalidOperationException($"impossible to deterine rowId type for PropertyRoute {pr} in path {reader.Path}");
                        var rowId = new PrimaryKey((IComparable)ReflectionTools.ChangeType(rowIdValue, rowIdType));

                        var oldValue = dic.GetOrThrow(rowId, "RowID {0} not found");
                       
                        T newValue = (T)serializer.DeserializeValue(reader, typeof(T), oldValue.Element);

                        if (oldValue.Element.Equals(newValue))
                            newList.Add(new MList<T>.RowIdElement(newValue, rowId, oldValue.OldIndex));
                        else
                            newList.Add(new MList<T>.RowIdElement(newValue));
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

            if (!AreEqual<T>(newList, existingValue == null ? null : existingValue.InnerList))
            {
                EntityJsonConverter.AssertCanWrite(pr);

                if (existingValue == null)
                    existingValue = new MList<T>();

                var added = newList.Select(a => a.Element).Except(existingValue.InnerList.Select(a => a.Element)).ToList();
                var removed = existingValue.InnerList.Select(a => a.Element).Except(newList.Select(a => a.Element)).ToList();

                existingValue.InnerList.Clear();
                existingValue.InnerList.AddRange(newList);
                existingValue.InnerListModified(added, removed);
            }
          

            return (MList<T>)existingValue;
        }

        static bool AreEqual<T>(List<MList<T>.RowIdElement> newList, List<MList<T>.RowIdElement> oldList)
        {
            if (newList.IsNullOrEmpty() && oldList.IsNullOrEmpty())
                return true;

            if (newList == null || oldList == null)
                return false;

            if (newList.Count != oldList.Count)
                return false;

            //Ordering the elements by RowId could remove some false modifications due to database indeterminism
            //but we can not be sure if order matters, and at the end the order from Json should be respected
            for (int i = 0; i < newList.Count; i++)
            {
                if (newList[i].RowId != oldList[i].RowId ||
                   !object.Equals(newList[i].Element, oldList[i].Element))
                    return false;
            }

            return true;
        }

        private static Type GetRowIdTypeFromSchema(PropertyRoute route)
        {
            if (!typeof(Entity).IsAssignableFrom(route.RootType))
                return null;

            var tryField = Schema.Current.TryField(route) as FieldMList;

            if (tryField == null)
                return null;

            return tryField.TableMList.PrimaryKey.Type;
        }



    }


}