using Newtonsoft.Json;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Signum.React.Json
{
    public class LiteJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Lite<IEntity>).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Lite<Entity> lite = (Lite<Entity>)value;
            writer.WriteStartObject();

            writer.WritePropertyName("EntityType");
            serializer.Serialize(writer, TypeLogic.GetCleanName(lite.EntityType));

            writer.WritePropertyName("id");
            serializer.Serialize(writer, lite.IdOrNull == null ? null : lite.Id.Object);

            writer.WritePropertyName("toStr");
            serializer.Serialize(writer, lite.ToString());

            if (lite.EntityOrNull != null)
            {
                writer.WritePropertyName("entity");
                serializer.Serialize(writer, lite.Entity);
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            reader.Assert(JsonToken.StartObject);

            string toString = null;
            string idObj = null;
            string typeStr = null;
            Entity entity = null;

            reader.Read();
            while (reader.TokenType == JsonToken.PropertyName)
            {
                switch ((string)reader.Value)
                {
                    case "toStr": toString = reader.ReadAsString(); break;
                    case "id": idObj = reader.ReadAsString(); break;
                    case "EntityType": typeStr = reader.ReadAsString(); break;
                    case "entity":
                        reader.Read();
                        entity = (Entity)serializer.Deserialize(reader, typeof(Entity));
                        break;
                    default: throw new InvalidOperationException("unexpected property " + (string)reader.Value);
                }

                reader.Read();
            }

            reader.Assert(JsonToken.EndObject);

            Type type = TypeLogic.GetType(typeStr);

            PrimaryKey? id = idObj == null ? (PrimaryKey?)null : PrimaryKey.Parse(idObj, type);

            if (entity == null)
                return Lite.Create(type, id.Value, toString);

            var result = entity.ToLite(entity.IsNew, toString);

            if (result.EntityType != type)
                throw new InvalidOperationException("Types don't match");

            if (result.Id != id)
                throw new InvalidOperationException("Id's don't match");

           return result;
        }
    }


}