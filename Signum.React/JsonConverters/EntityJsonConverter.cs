using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Newtonsoft.Json;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.React.Facades;
using Signum.React.Filters;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                .ToDictionary(a => a.PropertyValidator!.PropertyInfo.Name.FirstLower())
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

        public readonly IPropertyValidator? PropertyValidator;
        public readonly Func<object, object?>? GetValue;
        public readonly Action<object, object?>? SetValue;


        public Action<ReadJsonPropertyContext>? CustomReadJsonProperty { get; set; }
        public Action<WriteJsonPropertyContext>? CustomWriteJsonProperty { get; set; }

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
            return this.PropertyValidator?.PropertyInfo.Name ?? "";
        }

        internal bool IsNotNull()
        {
            var pi = this.PropertyValidator!.PropertyInfo;

            return pi.PropertyType.IsValueType && !pi.PropertyType.IsNullable();
        }
    }

    public class ReadJsonPropertyContext
    {
        public ReadJsonPropertyContext(JsonReader jsonReader, JsonSerializer jsonSerializer, PropertyConverter propertyConverter, ModifiableEntity entity, PropertyRoute parentPropertyRoute)
        {
            JsonReader = jsonReader;
            JsonSerializer = jsonSerializer;
            PropertyConverter = propertyConverter;
            Entity = entity;
            ParentPropertyRoute = parentPropertyRoute;
        }

        public JsonReader JsonReader { get; internal set; }
        public JsonSerializer JsonSerializer { get; internal set; }

        public PropertyConverter PropertyConverter { get; internal set; }
        public ModifiableEntity Entity { get; internal set; }
        public PropertyRoute ParentPropertyRoute { get; internal set; }
    }

    public class WriteJsonPropertyContext
    {
        public WriteJsonPropertyContext(ModifiableEntity entity, string lowerCaseName, PropertyConverter propertyConverter, PropertyRoute parentPropertyRoute, JsonWriter jsonWriter, JsonSerializer jsonSerializer)
        {
            Entity = entity;
            LowerCaseName = lowerCaseName;
            PropertyConverter = propertyConverter;
            ParentPropertyRoute = parentPropertyRoute;
            JsonWriter = jsonWriter;
            JsonSerializer = jsonSerializer;
        }

        public ModifiableEntity Entity { get; internal set; }
        public string LowerCaseName { get; internal set; }
        public PropertyConverter PropertyConverter { get; internal set; }
        public PropertyRoute ParentPropertyRoute { get; internal set; }

        public JsonWriter JsonWriter { get; internal set; }
        public JsonSerializer JsonSerializer { get; internal set; }
    }

    public class EntityJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ModifiableEntity).IsAssignableFrom(objectType) || typeof(IEntity).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            using (HeavyProfiler.LogNoStackTrace("WriteJson", () => value!.GetType().Name))
            {
                var tup = GetCurrentPropertyRoute(value!);

                ModifiableEntity mod = (ModifiableEntity)value!;

                writer.WriteStartObject();

                if (mod is Entity entity)
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

                foreach (var kvp in PropertyConverter.GetPropertyConverters(value!.GetType()))
                {
                    WriteJsonProperty(writer, serializer, mod, kvp.Key, kvp.Value, tup.pr);
                }

                var readonlyProps = PropertyConverter.GetPropertyConverters(value!.GetType())
                    .Where(kvp => kvp.Value.PropertyValidator?.IsPropertyReadonly(mod) == true)
                    .Select(a => a.Key)
                    .ToList();

                if (readonlyProps.Any())
                {
                    writer.WritePropertyName("readonlyProperties");
                    serializer.Serialize(writer, readonlyProps);
                }

                if (mod.Mixins.Any())
                {
                    writer.WritePropertyName("mixins");
                    writer.WriteStartObject();

                    foreach (var m in mod.Mixins)
                    {
                        var prm = tup.pr.Add(m.GetType());

                        using (JsonSerializerExtensions.SetCurrentPropertyRouteAndEntity((prm, m)))
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

        private static (PropertyRoute pr, ModifiableEntity? mod) GetCurrentPropertyRoute(object value)
        {
            var tup = JsonSerializerExtensions.CurrentPropertyRouteAndEntity;

            if (value is IRootEntity re)
                tup = (PropertyRoute.Root(value.GetType()), (ModifiableEntity)re);
            if (tup == null)
            {
                var controller = ((ControllerActionDescriptor)SignumCurrentContextFilter.CurrentContext!.ActionDescriptor);

                var embedded = (EmbeddedEntity)value;
                var att =  
                    controller.MethodInfo.GetCustomAttribute<EmbeddedPropertyRouteAttribute>() ??
                    controller.MethodInfo.DeclaringType!.GetCustomAttribute<EmbeddedPropertyRouteAttribute>() ??
                    throw new InvalidOperationException(@$"Impossible to determine PropertyRoute for {value.GetType().Name}. 
Consider adding someting like [EmbeddedPropertyRoute(typeof({embedded.GetType().Name}), typeof(SomeEntity), nameof(SomeEntity.SomeProperty))] to your action or controller.
Current action: {controller.MethodInfo.MethodSignature()}
Current controller: {controller.MethodInfo.DeclaringType!.FullName}");

                tup = (att.PropertyRoute, embedded);
            }
            else if (tup.Value.pr.Type.ElementType() == value.GetType())
                tup = (tup.Value.pr.Add("Item"), null); //We habe a custom MListConverter but not for other simple collections
            
            return tup.Value;
        }

        public static event Func<PropertyRoute, ModifiableEntity, string?>? CanReadPropertyRoute;

        public void WriteJsonProperty(JsonWriter writer, JsonSerializer serializer, ModifiableEntity mod, string lowerCaseName, PropertyConverter pc, PropertyRoute route)
        {
            if (pc.CustomWriteJsonProperty != null)
            {
                pc.CustomWriteJsonProperty(new WriteJsonPropertyContext(
                    entity : mod,
                    lowerCaseName : lowerCaseName,
                    propertyConverter : pc,
                    parentPropertyRoute : route,
                    jsonWriter : writer,
                    jsonSerializer  : serializer
                ));
            }
            else
            {
                var pr = route.Add(pc.PropertyValidator!.PropertyInfo);

                string? error = CanReadPropertyRoute?.Invoke(pr, mod);
                if (error != null)
                    return;

                using (JsonSerializerExtensions.SetCurrentPropertyRouteAndEntity((pr, mod)))
                {
                    writer.WritePropertyName(lowerCaseName);
                    var val = pc.GetValue!(mod);
                    if (val is Lite<Entity> lite)
                        new LiteJsonConverter().WriteJson(writer, lite, serializer);
                    else
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

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            using (HeavyProfiler.LogNoStackTrace("ReadJson", () => objectType.Name))
            {
                if (reader.TokenType == JsonToken.Null)
                    return null;

                using (EntityCache ec = new EntityCache())
                {
                    reader.Assert(JsonToken.StartObject);

                    ModifiableEntity mod = GetEntity(reader, objectType, existingValue, out bool markedAsModified);

                    var tup = GetCurrentPropertyRoute(mod);

                    var dic = PropertyConverter.GetPropertyConverters(mod.GetType());
                    using (JsonSerializerExtensions.SetAllowDirectMListChanges(markedAsModified))
                        while (reader.TokenType == JsonToken.PropertyName)
                        {
                            var propertyName = (string)reader.Value!;
                            if (propertyName == "mixins")
                            {
                                reader.Read();
                                reader.Assert(JsonToken.StartObject);

                                reader.Read();
                                while (reader.TokenType == JsonToken.PropertyName)
                                {
                                    var mixin = mod[(string)reader.Value!];

                                    reader.Read();

                                    using (JsonSerializerExtensions.SetCurrentPropertyRouteAndEntity((tup.pr.Add(mixin.GetType()), mixin)))
                                        serializer.DeserializeValue(reader, mixin.GetType(), mixin);

                                    reader.Read();
                                }

                                reader.Assert(JsonToken.EndObject);
                                reader.Read();
                            }
                            else if (propertyName == "readonlyProperties")
                            {
                                reader.Read();
                                serializer.Deserialize(reader, typeof(List<string>));
                                reader.Read();
                            }
                            else
                            {
                                PropertyConverter? pc = dic.TryGetC(propertyName);
                                if (pc == null)
                                {
                                    if (specialProps.Contains(propertyName))
                                        throw new InvalidOperationException($"Property '{propertyName}' is a special property like {specialProps.ToString(a => $"'{a}'", ", ")}, and they can only be at the beginning of the Json object for performance reasons");

                                    throw new KeyNotFoundException("Key '{0}' ({1}) not found on {2}".FormatWith(propertyName, propertyName.GetType().TypeName(), dic.GetType().TypeName()));
                                }

                                reader.Read();
                                ReadJsonProperty(reader, serializer, mod, pc, tup.pr, markedAsModified);

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
                pc.CustomReadJsonProperty(new ReadJsonPropertyContext(
                    jsonReader : reader,
                    jsonSerializer : serializer,
                    entity : entity,
                    parentPropertyRoute : parentRoute,
                    propertyConverter : pc
                ));
            }
            else
            {

                object? oldValue = pc.GetValue!(entity);

                var pi = pc.PropertyValidator!.PropertyInfo;

                var pr = parentRoute.Add(pi);

                using (JsonSerializerExtensions.SetCurrentPropertyRouteAndEntity((pr, entity)))
                {
                    object? newValue = serializer.DeserializeValue(reader, pi.PropertyType.Nullify(), oldValue);

                    if (!IsEquals(newValue, oldValue))
                    {
                        if (!markedAsModified && parentRoute.RootType.IsEntity())
                        {
                            if (!pi.HasAttribute<IgnoreAttribute>())
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

                        }
                        else
                        {
                            AssertCanWrite(pr, entity);
                            if (newValue == null && pc.IsNotNull())
                            {
                                entity.SetTemporalError(pi, ValidationMessage._0IsNotSet.NiceToString(pi.NiceName()));
                                return;
                            }

                            pc.SetValue?.Invoke(entity, newValue);
                        }
                    }
                }
            }
        }

        private bool IsEquals(object? newValue, object? oldValue)
        {
            if (newValue is byte[] nba && oldValue is byte[] oba)
                return MemoryExtensions.SequenceEqual<byte>(nba, oba);

            if (newValue is DateTime ndt && oldValue is DateTime odt)
                return Math.Abs(ndt.Subtract(odt).TotalMilliseconds) < 10; //Json dates get rounded

            if (newValue is DateTimeOffset ndto && oldValue is DateTimeOffset odto)
                return Math.Abs(ndto.Subtract(odto).TotalMilliseconds) < 10; //Json dates get rounded

            return object.Equals(newValue, oldValue);
        }


        public static event Func<PropertyRoute, ModifiableEntity?, string?>? CanWritePropertyRoute;
        public static void AssertCanWrite(PropertyRoute pr, ModifiableEntity? mod)
        {
            string? error = CanWritePropertyRoute.GetInvocationListTyped().Select(a => a(pr, mod)).NotNull().FirstOrDefault();
            if (error != null)
                throw new UnauthorizedAccessException(error);
        }

        public static Dictionary<Type, Func<ModifiableEntity>> CustomConstructor = new Dictionary<Type, Func<ModifiableEntity>>(); 

        public ModifiableEntity GetEntity(JsonReader reader, Type objectType, object? existingValue, out bool isModified)
        {
            IdentityInfo identityInfo = ReadIdentityInfo(reader);
            isModified = identityInfo.Modified == true;

            Type type = GetEntityType(identityInfo.Type, objectType);

            if (typeof(MixinEntity).IsAssignableFrom(objectType))
            {
                var mixin = (MixinEntity)existingValue!;

                return mixin;
            }

            if (identityInfo.IsNew == true)
            {
                var result = CustomConstructor.TryGetC(type)?.Invoke() ??
                    (ModifiableEntity)Activator.CreateInstance(type, nonPublic: true)!;

                if (identityInfo.Id != null)
                    ((Entity)result).SetId(PrimaryKey.Parse(identityInfo.Id, type));

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
                var existingMod = (ModifiableEntity?)existingValue;

                if (existingMod == null || existingMod.GetType() != type)
                    return (ModifiableEntity)Activator.CreateInstance(type, nonPublic: true)!;

                return existingMod;
            }
        }


        public IdentityInfo ReadIdentityInfo(JsonReader reader)
        {
            IdentityInfo info = new IdentityInfo();
            reader.Read();
            while (reader.TokenType == JsonToken.PropertyName)
            {
                switch ((string)reader.Value!)
                {
                    case "toStr": info.ToStr = reader.ReadAsString()!; break;
                    case "id": info.Id = reader.ReadAsString()!; break;
                    case "isNew": info.IsNew = reader.ReadAsBoolean(); break;
                    case "Type": info.Type = reader.ReadAsString()!; break;
                    case "ticks": info.Ticks = long.Parse(reader.ReadAsString()!); break;
                    case "modified": info.Modified = bool.Parse(reader.ReadAsString()!); break;
                    default: goto finish;
                }

                reader.Read();
            }

            finish:
            if (info.Type == null)
                throw new JsonSerializationException($"Expected member 'Type' not found in {reader.Path}");

            return info;
        }

        static readonly string[] specialProps = new string[] { "toStr", "id", "isNew", "Type", "ticks", "modified" };

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


    [System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public class EmbeddedPropertyRouteAttribute : Attribute
    {

        public Type EmbeddedType { get; private set; }
        public PropertyRoute PropertyRoute { get; private set; }
        // This is a positional argument
        public EmbeddedPropertyRouteAttribute(Type embeddedType, Type propertyRouteRoot, string propertyRouteText )
        {
            this.EmbeddedType = embeddedType;
            this.PropertyRoute = PropertyRoute.Parse(propertyRouteRoot, propertyRouteText);
        }
    }
}
