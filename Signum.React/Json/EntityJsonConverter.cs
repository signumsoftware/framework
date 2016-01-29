using Newtonsoft.Json;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Signum.React.Json
{
    public class PropertyConverter
    {
        public static ConcurrentDictionary<Type, List<PropertyConverter>> PropertyConverters = new ConcurrentDictionary<Type, List<PropertyConverter>>();

        public static List<PropertyConverter> GetPropertyConverters(Type type)
        {
            return PropertyConverters.GetOrAdd(type, _t =>
            Validator.GetPropertyValidators(_t).Values.Select(pv => new PropertyConverter(_t, pv)).ToList());
        }

        public readonly string LowercaseName; 
        public readonly IPropertyValidator PropertyValidator;
        public readonly Func<object, object> GetValue;
        public readonly Action<object, object> SetValue;

        public PropertyConverter(Type type, IPropertyValidator pv)
        {
            this.LowercaseName = pv.PropertyInfo.Name.FirstLower();
            this.PropertyValidator = pv;
            GetValue = ReflectionTools.CreateGetterUntyped(type, pv.PropertyInfo);
            SetValue = ReflectionTools.CreateSetterUntyped(type, pv.PropertyInfo);
        }

        public override string ToString()
        {
            return this.LowercaseName;
        }
    }


    public class EntityJsonConverter : JsonConverter
    {

        public override bool CanConvert(Type objectType)
        {
            return typeof(ModifiableEntity).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ModifiableEntity mod = (ModifiableEntity)value;

            writer.WriteStartObject();

            var entity = mod as Entity;
            if (entity != null)
            {
                writer.WritePropertyName("Type");
                serializer.Serialize(writer, TypeLogic.TryGetCleanName(mod.GetType()));

                writer.WritePropertyName("id");
                serializer.Serialize(writer, entity.IdOrNull == null ? null : entity.Id.Object);
                
                if (Schema.Current.Table(entity.GetType()).Ticks != null)
                {
                    writer.WritePropertyName("ticks");
                    serializer.Serialize(writer, entity.Ticks);
                }
            }
            else
            {
                writer.WritePropertyName("Type");
                serializer.Serialize(writer, mod.GetType().Name);
            }

            if (!(mod is MixinEntity))
            {
                writer.WritePropertyName("toStr");
                serializer.Serialize(writer, mod.ToString());
            }

            foreach (var pc in PropertyConverter.GetPropertyConverters(value.GetType()))
            {
                writer.WritePropertyName(pc.LowercaseName);
                serializer.Serialize(writer, pc.GetValue(mod));
            }
            
            if (entity != null && entity.Mixins.Any())
            {
                writer.WritePropertyName("mixins");
                writer.WriteStartObject();

                foreach (var m in entity.Mixins)
                {
                    writer.WritePropertyName(m.GetType().Name);
                    serializer.Serialize(writer, m);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Assert(reader, JsonToken.StartObject);

            throw new NotImplementedException();
        }

        private static void Assert(JsonReader reader, JsonToken expected)
        {
            if (reader.TokenType != expected)
                throw new InvalidOperationException("expected {0} but {1} found".FormatWith(expected, reader.TokenType));
        }
    }
}