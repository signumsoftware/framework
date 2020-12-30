using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Signum.Engine.Json
{
    public class PropertyConverter
    {
        public readonly IPropertyValidator? PropertyValidator;
        public readonly Func<ModifiableEntity, object?>? GetValue;
        public readonly Action<ModifiableEntity, object?>? SetValue;

        public ReadJsonPropertyDelegate? CustomReadJsonProperty { get; set; }
        public WriteJsonPropertyDelegate? CustomWriteJsonProperty { get; set; }

        public bool AvoidValidate { get; set; }

        public PropertyConverter()
        {
        }

        public PropertyConverter(Type type, IPropertyValidator pv)
        {
            this.PropertyValidator = pv;
            GetValue = ReflectionTools.CreateGetter<ModifiableEntity, object?>(pv.PropertyInfo);
            SetValue = ReflectionTools.CreateSetter<ModifiableEntity, object?>(pv.PropertyInfo);
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

    public delegate void ReadJsonPropertyDelegate(ref Utf8JsonReader reader, ReadJsonPropertyContext ctx);
    public class ReadJsonPropertyContext
    {
        public ReadJsonPropertyContext(JsonSerializerOptions jsonSerializerOptions, PropertyConverter propertyConverter, ModifiableEntity entity, PropertyRoute parentPropertyRoute, EntityJsonConverterFactory factory)
        {
            JsonSerializerOptions = jsonSerializerOptions;
            PropertyConverter = propertyConverter;
            Entity = entity;
            ParentPropertyRoute = parentPropertyRoute;
            Factory = factory;
        }

        public JsonSerializerOptions JsonSerializerOptions { get; internal set; }
        public PropertyConverter PropertyConverter { get; internal set; }
        public ModifiableEntity Entity { get; internal set; }
        public PropertyRoute ParentPropertyRoute { get; internal set; }
        public EntityJsonConverterFactory Factory { get; set; }
    }

    public delegate void WriteJsonPropertyDelegate(Utf8JsonWriter writer, WriteJsonPropertyContext ctx);
    public class WriteJsonPropertyContext
    {
        public WriteJsonPropertyContext(ModifiableEntity entity, string lowerCaseName, PropertyConverter propertyConverter, PropertyRoute parentPropertyRoute, JsonSerializerOptions jsonSerializerOptions, EntityJsonConverterFactory factory)
        {
            Entity = entity;
            LowerCaseName = lowerCaseName;
            PropertyConverter = propertyConverter;
            ParentPropertyRoute = parentPropertyRoute;
            JsonSerializerOptions = jsonSerializerOptions;
            Factory = factory;
        }

        public ModifiableEntity Entity { get; internal set; }
        public string LowerCaseName { get; internal set; }
        public PropertyConverter PropertyConverter { get; internal set; }
        public PropertyRoute ParentPropertyRoute { get; internal set; }

        public JsonSerializerOptions JsonSerializerOptions { get; internal set; }
        public EntityJsonConverterFactory Factory { get; set; }
    }

    public static class EntityJsonConverter
    {
     
    }

    public enum EntityJsonConverterStrategy
    {
        /// <summary>
        /// Only the visible entites are serialized, when deserializing a retrieve is made to apply changes. Default for Web.Api Request / Response
        /// </summary>
        WebAPI,
        /// <summary>
        /// Serialized and deserialized as-is. Usefull for files and Auth Tokens. 
        /// </summary>
        Full,
    }

    public class EntityJsonConverterFactory: JsonConverterFactory
    {
        public Polymorphic<Action<ModifiableEntity>> AfterDeserilization = new Polymorphic<Action<ModifiableEntity>>();
        public Dictionary<Type, Func<ModifiableEntity>> CustomConstructor = new Dictionary<Type, Func<ModifiableEntity>>();
        public Func<PropertyRoute, ModifiableEntity?, string?>? CanWritePropertyRoute;
        public Func<PropertyRoute, ModifiableEntity, string?>? CanReadPropertyRoute;
        public Func<PropertyInfo, Exception, string?> GetErrorMessage = (pi, ex) => "Unexpected error";

        public void AssertCanWrite(PropertyRoute pr, ModifiableEntity? mod)
        {
            string? error = CanWritePropertyRoute.GetInvocationListTyped().Select(a => a(pr, mod)).NotNull().FirstOrDefault();
            if (error != null)
                throw new UnauthorizedAccessException(error);
        }

        public EntityJsonConverterFactory()
        {
            AfterDeserilization.Register((ModifiableEntity e) => { });
        }

        public ConcurrentDictionary<Type, Dictionary<string, PropertyConverter>> PropertyConverters = new ConcurrentDictionary<Type, Dictionary<string, PropertyConverter>>();

        public Dictionary<string, PropertyConverter> GetPropertyConverters(Type type)
        {
            return PropertyConverters.GetOrAdd(type, _t =>
                Validator.GetPropertyValidators(_t).Values
                .Where(pv => ShouldSerialize(pv.PropertyInfo))
                .Select(pv => new PropertyConverter(_t, pv))
                .ToDictionary(a => a.PropertyValidator!.PropertyInfo.Name.FirstLower())
            );
        }

        protected virtual bool ShouldSerialize(PropertyInfo pi)
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

        public virtual (PropertyRoute pr, ModifiableEntity? mod) GetCurrentPropertyRoute(ModifiableEntity mod)
        {
            var tup = EntityJsonContext.CurrentPropertyRouteAndEntity;

            if (mod is IRootEntity re)
                tup = (PropertyRoute.Root(mod.GetType()), mod);
            if (tup == null)
            {
                var embedded = (EmbeddedEntity)mod;
                var route = GetCurrentPropertyRouteEmbedded(embedded);
                return (route, embedded);
            }
            else if (tup.Value.pr.Type.ElementType() == mod.GetType())
                tup = (tup.Value.pr.Add("Item"), null); //We have a custom MListConverter but not for other simple collections

            return tup.Value;
        }

        protected virtual PropertyRoute GetCurrentPropertyRouteEmbedded(EmbeddedEntity embedded)
        {
            throw new InvalidOperationException(@$"Impossible to determine PropertyRoute for {embedded.GetType().Name}.");
        }

        public virtual Type ResolveType(string typeStr, Type objectType)
        {
            if (objectType.Name == typeStr || Reflector.CleanTypeName(objectType) == typeStr)
                return objectType;

            var type = TypeLogic.GetType(typeStr);

            if (type.IsEnum)
                type = EnumEntity.Generate(type);

            if (!objectType.IsAssignableFrom(type))
                throw new JsonException($"Type '{type.Name}' is not assignable to '{objectType.TypeName()}'");

            return type;
        }

        public virtual EntityJsonConverterStrategy Strategy => EntityJsonConverterStrategy.Full;

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(IModifiableEntity).IsAssignableFrom(typeToConvert);
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter)Activator.CreateInstance(typeof(EntityJsonConverter<>).MakeGenericType(typeToConvert), this)!;
        }

        static AsyncLocal<string?> path = new AsyncLocal<string?>();

        public static string? CurrentPath => path.Value;
        public static  IDisposable SetPath(string newPart)
        {
            var oldPart = path.Value;
            path.Value = oldPart + newPart;
            return new Disposable(() => path.Value = oldPart);
        }
    }

    public class EntityJsonConverter<T> : JsonConverterWithExisting<T>
        where T : class, IModifiableEntity
    {

        public EntityJsonConverterFactory Factory { get; }
        public EntityJsonConverter(EntityJsonConverterFactory factory)
        {
            Factory = factory;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            using (HeavyProfiler.LogNoStackTrace("WriteJson", () => value!.GetType().Name))
            {
                var tup = Factory.GetCurrentPropertyRoute((ModifiableEntity)(IModifiableEntity)value!);

                ModifiableEntity mod = (ModifiableEntity)(IModifiableEntity)value!;

                writer.WriteStartObject();

                if (mod is Entity entity)
                {
                    writer.WriteString("Type", TypeLogic.TryGetCleanName(mod.GetType()));

                    writer.WritePropertyName("id");
                    JsonSerializer.Serialize(writer, entity.IdOrNull?.Object, entity.IdOrNull?.Object.GetType() ?? typeof(object), options);

                    if (entity.IsNew)
                    {
                        writer.WriteBoolean("isNew", true);
                    }

                    if (Schema.Current.Table(entity.GetType()).Ticks != null)
                    {
                        writer.WriteString("ticks", entity.Ticks.ToString());
                    }
                }
                else
                {
                    writer.WriteString("Type", mod.GetType().Name);
                }

                if (!(mod is MixinEntity))
                {
                    writer.WriteString("toStr", mod.ToString());
                }

                writer.WriteBoolean("modified", mod.Modified == ModifiedState.Modified || mod.Modified == ModifiedState.SelfModified);

                foreach (var kvp in Factory.GetPropertyConverters(value!.GetType()))
                {
                    WriteJsonProperty(writer, options, mod, kvp.Key, kvp.Value, tup.pr);
                }

                var readonlyProps = Factory.GetPropertyConverters(value!.GetType())
                    .Where(kvp => kvp.Value.PropertyValidator?.IsPropertyReadonly(mod) == true)
                    .Select(a => a.Key)
                    .ToList();

                if (readonlyProps.Any())
                {
                    writer.WritePropertyName("readonlyProperties");
                    JsonSerializer.Serialize(writer, readonlyProps, readonlyProps.GetType(), options);
                }

                if (mod.Mixins.Any())
                {
                    writer.WritePropertyName("mixins");
                    writer.WriteStartObject();

                    foreach (var m in mod.Mixins)
                    {
                        var prm = tup.pr.Add(m.GetType());

                        using (EntityJsonContext.SetCurrentPropertyRouteAndEntity((prm, m)))
                        {
                            writer.WritePropertyName(m.GetType().Name);
                            JsonSerializer.Serialize(writer, m, options);
                        }
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }
        }

        public void WriteJsonProperty(Utf8JsonWriter writer, JsonSerializerOptions options, ModifiableEntity mod, string lowerCaseName, PropertyConverter pc, PropertyRoute route)
        {
            if (pc.CustomWriteJsonProperty != null)
            {
                pc.CustomWriteJsonProperty(writer, new WriteJsonPropertyContext(
                    entity : mod,
                    lowerCaseName : lowerCaseName,
                    propertyConverter : pc,
                    parentPropertyRoute : route,
                    jsonSerializerOptions: options,
                    factory: this.Factory
                ));
            }
            else
            {
                var pr = route.Add(pc.PropertyValidator!.PropertyInfo);

                if (Factory.Strategy == EntityJsonConverterStrategy.WebAPI)
                {
                    string? error = Factory.CanReadPropertyRoute?.Invoke(pr, mod);
                    if (error != null)
                        return;
                }

                using (EntityJsonContext.SetCurrentPropertyRouteAndEntity((pr, mod)))
                {
                    writer.WritePropertyName(lowerCaseName);
                    var val = pc.GetValue!(mod);
                    if (val is null)
                        writer.WriteNullValue();
                    else
                        JsonSerializer.Serialize(writer, val, val.GetType(), options);
                }
            }
        }


  

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, T? existingValue)
        {
            using (HeavyProfiler.LogNoStackTrace("ReadJson", () => typeToConvert.Name))
            {
                if (reader.TokenType == JsonTokenType.Null)
                    return null;

                using (EntityCache ec = new EntityCache())
                {
                    reader.Assert(JsonTokenType.StartObject);

                    ModifiableEntity mod = GetEntity(ref reader, typeToConvert, existingValue, out bool markedAsModified);

                    var tup = Factory.GetCurrentPropertyRoute(mod);

                    var dic = Factory.GetPropertyConverters(mod.GetType());
                    using (EntityJsonContext.SetAllowDirectMListChanges(Factory.Strategy == EntityJsonConverterStrategy.Full || markedAsModified))
                        while (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            var propertyName = reader.GetString()!;
                            using (EntityJsonConverterFactory.SetPath("." + propertyName))
                            {
                                if (propertyName == "mixins")
                                {
                                    reader.Read();
                                    reader.Assert(JsonTokenType.StartObject);

                                    reader.Read();
                                    while (reader.TokenType == JsonTokenType.PropertyName)
                                    {
                                        var mixin = mod[reader.GetString()!];

                                        reader.Read();

                                        var converter = (IJsonConverterWithExisting)options.GetConverter(mixin.GetType());
                                        using (EntityJsonContext.SetCurrentPropertyRouteAndEntity((tup.pr.Add(mixin.GetType()), mixin)))
                                            converter.Read(ref reader, mixin.GetType(), options, mixin);

                                        reader.Read();
                                    }

                                    reader.Assert(JsonTokenType.EndObject);
                                    reader.Read();
                                }
                                else if (propertyName == "readonlyProperties")
                                {
                                    reader.Read();
                                    JsonSerializer.Deserialize(ref reader, typeof(List<string>), options);
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
                                    ReadJsonProperty(ref reader, options, mod, pc, tup.pr, markedAsModified);

                                    reader.Read();
                                }
                            }
                        }

                    reader.Assert(JsonTokenType.EndObject);

                    Factory.AfterDeserilization.Invoke(mod);

                    return (T)(IModifiableEntity)mod;
                }
            }
        }

        public void ReadJsonProperty(ref Utf8JsonReader reader, JsonSerializerOptions options, ModifiableEntity entity, PropertyConverter pc, PropertyRoute parentRoute, bool markedAsModified)
        {
            if (pc.CustomReadJsonProperty != null)
            {
                pc.CustomReadJsonProperty(ref reader, new ReadJsonPropertyContext(
                    jsonSerializerOptions: options,
                    entity : entity,
                    parentPropertyRoute : parentRoute,
                    propertyConverter : pc,
                    factory: this.Factory
                ));
            }
            else
            {
                object? oldValue = pc.GetValue!(entity);

                var pi = pc.PropertyValidator!.PropertyInfo;

                var pr = parentRoute.Add(pi);

                using (EntityJsonContext.SetCurrentPropertyRouteAndEntity((pr, entity)))
                {
                    try
                    {
                        var converter = options.GetConverter(pi.PropertyType) as IJsonConverterWithExisting;

                        object? newValue = converter != null ?
                            converter.Read(ref reader, pi.PropertyType, options, oldValue) :
                            JsonSerializer.Deserialize(ref reader, pi.PropertyType, options);

                        if (Factory.Strategy == EntityJsonConverterStrategy.Full)
                        {
                            pc.SetValue?.Invoke(entity, newValue);
                        }
                        else
                        {
                            if (pi.CanWrite)
                            {
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
                                        Factory.AssertCanWrite(pr, entity);
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
                    catch (Exception e)
                    {
                        switch (reader.TokenType)
                        {
                            //Probably won't be able to continue deserialization
                            case JsonTokenType.None:
                            case JsonTokenType.StartObject:
                            case JsonTokenType.StartArray:
                            case JsonTokenType.PropertyName:
                                throw;

                            //Probably will be able to continue
                            case JsonTokenType.EndObject:
                            case JsonTokenType.EndArray:
                            case JsonTokenType.Comment:
                            case JsonTokenType.String:
                            case JsonTokenType.Number:
                            case JsonTokenType.True:
                            case JsonTokenType.False:
                            case JsonTokenType.Null:
                            default:
                                {
                                    e.LogException();
                                    entity.SetTemporalError(pi, this.Factory.GetErrorMessage(pi, e));
                                    break;
                                }
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


        public ModifiableEntity GetEntity(ref Utf8JsonReader reader, Type objectType, IModifiableEntity? existingValue, out bool isModified)
        {
            IdentityInfo identityInfo = ReadIdentityInfo(ref reader);
            isModified = identityInfo.Modified == true;

            Type type = Factory.ResolveType(identityInfo.Type, objectType);

            if (typeof(MixinEntity).IsAssignableFrom(objectType))
            {
                var mixin = (MixinEntity)existingValue!;

                return mixin;
            }

            if (identityInfo.IsNew == true)
            {
                var result = Factory.CustomConstructor.TryGetC(type)?.Invoke() ??
                    (ModifiableEntity)Activator.CreateInstance(type, nonPublic: true)!;

                if (identityInfo.Id != null)
                    ((Entity)result).SetId(PrimaryKey.Parse(identityInfo.Id, type));

                return result;
            }

            if (typeof(Entity).IsAssignableFrom(type))
            {
                if (identityInfo.Id == null)
                    throw new JsonException($"Missing Id and IsNew for {identityInfo} ({reader.CurrentState})");

                var id = PrimaryKey.Parse(identityInfo.Id, type);
                if (existingValue != null && existingValue.GetType() == type)
                {
                    Entity existingEntity = (Entity)existingValue;
                    if (existingEntity.Id == id)
                    {                        
                        if (identityInfo.Ticks != null)
                        {
                            if (identityInfo.Modified == true && existingEntity.Ticks != identityInfo.Ticks.Value)
                                throw new ConcurrencyException(type, id);

                            existingEntity.Ticks = identityInfo.Ticks.Value;
                        }

                        return existingEntity;
                    }
                }

                if (Factory.Strategy == EntityJsonConverterStrategy.WebAPI)
                {
                    var retrievedEntity = Database.Retrieve(type, id);
                    if (identityInfo.Ticks != null)
                    {
                        if (identityInfo.Modified == true && retrievedEntity.Ticks != identityInfo.Ticks.Value)
                            throw new ConcurrencyException(type, id); 

                        retrievedEntity.Ticks = identityInfo.Ticks.Value;
                    }

                    return retrievedEntity;
                }
                else
                {
                    var result = (Entity?)Factory.CustomConstructor.TryGetC(type)?.Invoke() ??
                     (Entity)Activator.CreateInstance(type, nonPublic: true)!;

                    if (identityInfo.Id != null)
                        ((Entity)result).SetId(PrimaryKey.Parse(identityInfo.Id, type));

                    if (!identityInfo.Modified!.Value)
                        ((Entity)result).SetCleanModified(false);

                    if (identityInfo.IsNew != true)
                        ((Entity)result).SetIsNew(false);

                    if (identityInfo.Ticks != null)
                        result.Ticks = identityInfo.Ticks.Value;

                    return result;
                }

            }
            else //Embedded
            {
                var existingMod = (ModifiableEntity?)existingValue;

                if (existingMod == null || existingMod.GetType() != type)
                    return (ModifiableEntity)Activator.CreateInstance(type, nonPublic: true)!;

                return existingMod;
            }
        }


        public IdentityInfo ReadIdentityInfo(ref Utf8JsonReader reader)
        {
            IdentityInfo info = new IdentityInfo();
            reader.Read();
            while (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propName = reader.GetString();
                switch (propName)
                {
                    case "toStr": reader.Read(); info.ToStr = reader.GetString()!; break;
                    case "id": 
                        {
                            reader.Read();
                            info.Id = reader.GetLiteralValue()?.ToString();
                        }
                        break;
                    case "isNew": reader.Read(); info.IsNew = reader.GetBoolean(); break;
                    case "Type": reader.Read(); info.Type = reader.GetString()!; break;
                    case "ticks": reader.Read(); info.Ticks = long.Parse(reader.GetString()!); break;
                    case "modified": reader.Read(); info.Modified = reader.GetBoolean(); break;
                    default: goto finish;
                }

                reader.Read();
            }

            finish:
            if (info.Type == null)
                throw new JsonException($"Expected member 'Type' not found in {reader.CurrentState}");

            return info;
        }

        static readonly string[] specialProps = new string[] { "toStr", "id", "isNew", "Type", "ticks", "modified" };

      
    }

    public struct IdentityInfo
    {
        public string? Id;
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
}
