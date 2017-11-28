using Newtonsoft.Json;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.React.Facades;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Signum.React.Json
{
    public class PropertyConverter
    {
        public static ConcurrentDictionary<Type, Dictionary<string, PropertyConverter>> PropertyConverters = new ConcurrentDictionary<Type, Dictionary<string, PropertyConverter>>();

        public static Dictionary<string, PropertyConverter> GetPropertyConverters(Type type)
        {
            return PropertyConverters.GetOrAdd(type, _t =>
                Validator.GetPropertyValidators(_t).Values
                .Where(pv => ShouldSerialize(pv.PropertyInfo))
                .Select(pv => new PropertyConverter(_t, pv))
                .ToDictionary(a => a.PropertyValidator.PropertyInfo.Name.FirstLower())
            );
        }

        static bool ShouldSerialize(PropertyInfo pi)
        {
            var ts = pi.GetCustomAttribute<InTypeScriptAttribute>();
            if (ts != null)
            {
                var v = ts.GetInTypeScript();

                if (v.HasValue)
                    return v.Value;
            }
            if (pi.HasAttribute<HiddenPropertyAttribute>() || pi.HasAttribute<ExpressionFieldAttribute>())
                return false;

            return true;
        }

        public readonly IPropertyValidator PropertyValidator;
        public readonly Func<object, object> GetValue;
        public readonly Action<object, object> SetValue;


        public Action<ReadJsonPropertyContext> CustomReadJsonProperty { get; set; }
        public Action<WriteJsonPropertyContext> CustomWriteJsonProperty { get; set; }

        public bool AvoidValidate { get; set; }

        public PropertyConverter()
        {
        }

        public PropertyConverter(Type type, IPropertyValidator pv)
        {
            this.PropertyValidator = pv;
            GetValue = ReflectionTools.CreateGetterUntyped(type, pv.PropertyInfo);
            SetValue = ReflectionTools.CreateSetterUntyped(type, pv.PropertyInfo);
        }

        public override string ToString()
        {
            return this.PropertyValidator?.PropertyInfo.Name;
        }

        internal bool IsNotNull()
        {
            var pi = this.PropertyValidator.PropertyInfo;

            return pi.PropertyType.IsValueType && !pi.PropertyType.IsNullable();
        }
    }

    public class ReadJsonPropertyContext
    {
        public JsonReader JsonReader { get; internal set; }
        public JsonSerializer JsonSerializer { get; internal set; }

        public PropertyConverter PropertyConverter { get; internal set; }
        public ModifiableEntity Entity { get; internal set; }
        public PropertyRoute ParentPropertyRoute { get; internal set; }
    }

    public class WriteJsonPropertyContext
    {
        public ModifiableEntity Entity { get; internal set; }
        public string LowerCaseName { get; internal set; }
        public PropertyConverter PropertyConverter { get; internal set; }
        public PropertyRoute ParentPropertyRoute { get; internal set; }

        public JsonWriter JsonWriter { get; internal set; }
        public JsonSerializer JsonSerializer { get; internal set; }
    }

    public class EntityJsonConverter : JsonConverter
    {
        public static Dictionary<Type, PropertyRoute> DefaultPropertyRoutes = new Dictionary<Type, PropertyRoute>();

        public override bool CanConvert(Type objectType)
        {
            return typeof(ModifiableEntity).IsAssignableFrom(objectType) || typeof(IEntity).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            using (HeavyProfiler.LogNoStackTrace("WriteJson", () => value.GetType().Name))
            {
                PropertyRoute pr = GetCurrentPropertyRoute(value);

                ModifiableEntity mod = (ModifiableEntity)value;

                writer.WriteStartObject();

                var entity = mod as Entity;
                if (entity != null)
                {
                    writer.WritePropertyName("Type");
                    writer.WriteValue(TypeLogic.TryGetCleanName(mod.GetType()));

                    writer.WritePropertyName("id");
                    writer.WriteValue(entity.IdOrNull == null ? null : entity.Id.Object);

                    if (entity.IsNew)
                    {
                        writer.WritePropertyName("isNew");
                        writer.WriteValue(true);
                    }

                    if (Schema.Current.Table(entity.GetType()).Ticks != null)
                    {
                        writer.WritePropertyName("ticks");
                        writer.WriteValue(entity.Ticks.ToString());
                    }
                }
                else
                {
                    writer.WritePropertyName("Type");
                    writer.WriteValue(mod.GetType().Name);
                }

                if (!(mod is MixinEntity))
                {
                    writer.WritePropertyName("toStr");
                    writer.WriteValue(mod.ToString());
                }

                writer.WritePropertyName("modified");
                writer.WriteValue(mod.Modified == ModifiedState.Modified || mod.Modified == ModifiedState.SelfModified);

                foreach (var kvp in PropertyConverter.GetPropertyConverters(value.GetType()))
                {
                    WriteJsonProperty(writer, serializer, mod, kvp.Key, kvp.Value, pr);
                }

                if (entity != null && entity.Mixins.Any())
                {
                    writer.WritePropertyName("mixins");
                    writer.WriteStartObject();

                    foreach (var m in entity.Mixins)
                    {
                        var prm = pr.Add(m.GetType());

                        using (JsonSerializerExtensions.SetCurrentPropertyRoute(prm))
                        {
                            writer.WritePropertyName(m.GetType().Name);
                            serializer.Serialize(writer, m);
                        }
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }
        }

        private static PropertyRoute GetCurrentPropertyRoute(object value)
        {
            var pr = JsonSerializerExtensions.CurrentPropertyRoute;

            if (value is IRootEntity)
                pr = PropertyRoute.Root(value.GetType());
            if (pr == null)
            {
                var embedded = (EmbeddedEntity)value;
                pr = DefaultPropertyRoutes.TryGetC(embedded.GetType()) ?? 
                    throw new InvalidOperationException($"Impossible to determine PropertyRoute for {value.GetType().Name}. Consider adding a new value to {nameof(EntityJsonConverter)}.{nameof(EntityJsonConverter.DefaultPropertyRoutes)}.");
            }
            else if (pr.Type.ElementType() == value.GetType())
                pr = pr.Add("Item"); //We habe a custom MListConverter but not for other simple collections
            return pr;
        }

        public static Func<PropertyRoute, string> CanReadPropertyRoute;

        public void WriteJsonProperty(JsonWriter writer, JsonSerializer serializer, ModifiableEntity mod, string lowerCaseName, PropertyConverter pc, PropertyRoute route)
        {
            if (pc.CustomWriteJsonProperty != null)
            {
                pc.CustomWriteJsonProperty(new WriteJsonPropertyContext
                {
                    JsonWriter = writer,
                    JsonSerializer = serializer,
                    LowerCaseName = lowerCaseName,
                    Entity = mod,
                    ParentPropertyRoute = route,
                    PropertyConverter = pc
                });
            }
            else
            {
                var pr = route.Add(pc.PropertyValidator.PropertyInfo);

                string error = CanReadPropertyRoute?.Invoke(pr);

                if (error != null)
                    return;

                using (JsonSerializerExtensions.SetCurrentPropertyRoute(pr))
                {
                    writer.WritePropertyName(lowerCaseName);
                    serializer.Serialize(writer, pc.GetValue(mod));
                    if (writer.WriteState == WriteState.Property)
                        throw new InvalidOperationException($"Impossible to serialize '{mod}' to JSON. Maybe there is a cycle?");
                }
            }
        }


        public static Polymorphic<Action<ModifiableEntity>> AfterDeserilization = new Polymorphic<Action<ModifiableEntity>>();

        static EntityJsonConverter()
        {
            AfterDeserilization.Register((ModifiableEntity e) => { });
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            using (HeavyProfiler.LogNoStackTrace("ReadJson", () => objectType.Name))
            {
                if (reader.TokenType == JsonToken.Null)
                    return null;

                using (EntityCache ec = new EntityCache())
                {
                    reader.Assert(JsonToken.StartObject);

                    ModifiableEntity mod = GetEntity(reader, objectType, existingValue, serializer, out bool markedAsModified);

                    var pr = GetCurrentPropertyRoute(mod);

                    var dic = PropertyConverter.GetPropertyConverters(mod.GetType());

                    while (reader.TokenType == JsonToken.PropertyName)
                    {
                        if ((string)reader.Value == "mixins")
                        {
                            var entity = (Entity)mod;
                            reader.Read();
                            reader.Assert(JsonToken.StartObject);

                            reader.Read();
                            while (reader.TokenType == JsonToken.PropertyName)
                            {
                                var mixin = entity[(string)reader.Value];

                                reader.Read();

                                using (JsonSerializerExtensions.SetCurrentPropertyRoute(pr.Add(mixin.GetType())))
                                    serializer.DeserializeValue(reader, mixin.GetType(), mixin);

                                reader.Read();
                            }

                            reader.Assert(JsonToken.EndObject);
                            reader.Read();
                        }
                        else
                        {
                            PropertyConverter pc = dic.GetOrThrow((string)reader.Value);

                            reader.Read();
                            ReadJsonProperty(reader, serializer, mod, pc, pr, markedAsModified);

                            reader.Read();
                        }
                    }

                    reader.Assert(JsonToken.EndObject);

                    AfterDeserilization.Invoke(mod);

                    return mod;
                }
            }
        }

        public void ReadJsonProperty(JsonReader reader, JsonSerializer serializer, ModifiableEntity entity, PropertyConverter pc, PropertyRoute parentRoute, bool markedAsModified)
        {
            if (pc.CustomReadJsonProperty != null)
            {
                pc.CustomReadJsonProperty(new ReadJsonPropertyContext
                {
                    JsonReader = reader,
                    JsonSerializer = serializer,
                    Entity = entity,
                    ParentPropertyRoute = parentRoute,
                    PropertyConverter = pc,
                });
            }
            else
            {

                object oldValue = pc.GetValue(entity);

                var pi = pc.PropertyValidator.PropertyInfo;

                var pr = parentRoute.Add(pi);

                using (JsonSerializerExtensions.SetCurrentPropertyRoute(pr))
                {
                    object newValue = serializer.DeserializeValue(reader, pi.PropertyType, oldValue);

                    if (!IsEquals(newValue, oldValue))
                    {
                        if (!markedAsModified && parentRoute.RootType.IsEntity())
                        {
                            try
                            {
                                //Call attention of developer
                                throw new InvalidOperationException($"'modified' is not set but '{pi.Name}' is modified");
                            }
                            catch (Exception)
                            {
                            }

                        }
                        else
                        {
                            AssertCanWrite(pr);
                            if (newValue == null && pc.IsNotNull()) //JSON.Net already complaining
                                return;

                            pc.SetValue?.Invoke(entity, newValue);
                        }
                    }
                }
            }
        }

        private bool IsEquals(object newValue, object oldValue)
        {
            if (newValue is byte[] && oldValue is byte[])
                return MemCompare.Compare((byte[])newValue, (byte[])oldValue);

            if (newValue is DateTime && oldValue is DateTime)
                return Math.Abs(((DateTime)newValue).Subtract((DateTime)oldValue).TotalMilliseconds) < 10; //JSon dates get rounded

            return object.Equals(newValue, oldValue);
        }


        public static Func<PropertyRoute, string> CanWritePropertyRoute;
        public static void AssertCanWrite(PropertyRoute pr)
        {
            string error = CanWritePropertyRoute?.Invoke(pr);
            if (error != null)
                throw new UnauthorizedAccessException(error);
        }

        public ModifiableEntity GetEntity(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer, out bool isModified)
        {
            IdentityInfo identityInfo = ReadIdentityInfo(reader);
            isModified = identityInfo.Modified == true;

            Type type = GetEntityType(identityInfo.Type, objectType);

            if (typeof(MixinEntity).IsAssignableFrom(objectType))
            {
                var mixin = (MixinEntity)existingValue;

                return mixin;
            }

            if (identityInfo.IsNew == true)
            {
                var result = (ModifiableEntity)Activator.CreateInstance(type, nonPublic: true);

                if (identityInfo.Id != null)
                    ((Entity)result).Id = PrimaryKey.Parse(identityInfo.Id, type);

                return result;
            }

            if (typeof(Entity).IsAssignableFrom(type))
            {
                if (identityInfo.Id == null)
                    throw new JsonSerializationException($"Missing Id and IsNew for {identityInfo} ({reader.Path})");

                var id = PrimaryKey.Parse(identityInfo.Id, type);
                if (existingValue != null && existingValue.GetType() == type)
                {
                    Entity existingEntity = (Entity)existingValue;
                    if (existingEntity.Id == id)
                    {
                        if (identityInfo.Ticks != null)
                            existingEntity.Ticks = identityInfo.Ticks.Value;

                        return existingEntity;
                    }
                }

                var retrievedEntity = Database.Retrieve(type, id);
                if (identityInfo.Ticks != null)
                    retrievedEntity.Ticks = identityInfo.Ticks.Value;

                return retrievedEntity;
            }
            else //Embedded
            {
                var existingMod = (ModifiableEntity)existingValue;

                if (existingMod == null || existingMod.GetType() != type)
                    return (ModifiableEntity)Activator.CreateInstance(type, nonPublic: true);

                return existingMod;
            }
        }

 
        public IdentityInfo ReadIdentityInfo(JsonReader reader)
        {
            IdentityInfo info = new IdentityInfo();
            reader.Read();
            while (reader.TokenType == JsonToken.PropertyName)
            {
                switch ((string)reader.Value)
                {
                    case "toStr": info.ToStr = reader.ReadAsString(); break;
                    case "id": info.Id = reader.ReadAsString(); break;
                    case "isNew": info.IsNew = reader.ReadAsBoolean(); break;
                    case "Type": info.Type = reader.ReadAsString(); break;
                    case "ticks": info.Ticks = long.Parse(reader.ReadAsString()); break;
                    case "modified": info.Modified = bool.Parse(reader.ReadAsString()); break;
                    default: return info;
                }

                reader.Read();
            }

            if (info.Type == null)
                throw new JsonSerializationException($"Expected member 'Type' not found in {reader.Path}");

            return info;
        }

        public struct IdentityInfo
        {
            public string Id;
            public bool? IsNew;
            public bool? Modified; 
            public string Type;
            public string ToStr;
            public long? Ticks;

            public override string ToString()
            {
                var newOrId = IsNew == true ? "New" : Id;

                if (Ticks != null)
                    newOrId += $" (Ticks {Ticks})";

                return $"{Type} {newOrId}: {ToStr}";
            }
        }

        static Type GetEntityType(string typeStr, Type objectType)
        {
            var type = ReflectionServer.TypesByName.Value.GetOrThrow(typeStr);

            if (type.IsEnum)
                type = EnumEntity.Generate(type);

            if (!objectType.IsAssignableFrom(type))
                throw new JsonSerializationException($"Type '{type.Name}' is not assignable to '{objectType.TypeName()}'");

            return type;

        }
    }


    static class MemCompare
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
#pragma warning disable IDE1006 // Naming Styles
        static extern int memcmp(byte[] b1, byte[] b2, long count);
#pragma warning restore IDE1006 // Naming Styles

        public static bool Compare(byte[] b1, byte[] b2)
        {
            // Validate buffers are the same length.
            // This also ensures that the count does not exceed the length of either buffer.  
            return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
        }
    }
}