using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Entities.Dynamic
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class DynamicTypeEntity : Entity
    {
        public DynamicBaseType BaseType { set; get; }

        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100), IdentifierValidator(IdentifierType.PascalAscii)]
        public string TypeName { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        string typeDefinition;
        [StringLengthValidator(Min = 3)]
        public string TypeDefinition
        {
            get { return this.Get(typeDefinition); }
            set
            {
                if (this.Set(ref typeDefinition, value))
                    this.definition = null;
            }
        }

        [Ignore]
        DynamicTypeDefinition? definition;
        public DynamicTypeDefinition GetDefinition()
        {
            return definition ?? JsonConvert.DeserializeObject<DynamicTypeDefinition>(this.TypeDefinition);
        }

        public void SetDefinition(DynamicTypeDefinition definition)
        {
            this.TypeDefinition = JsonConvert.SerializeObject(definition);
            this.definition = definition;
        }

        protected override string? PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(TypeDefinition))
            {
                var def = this.GetDefinition();

                return def.Properties
                    .Where(p => p.Name.HasText() && !IdentifierValidatorAttribute.PascalAscii.IsMatch(p.Name))
                    .Select(p => ValidationMessage._0DoesNotHaveAValid1IdentifierFormat.NiceToString(p.Name, IdentifierType.PascalAscii))
                    .ToString("\r\n")
                    .DefaultToNull();
            }
            return base.PropertyValidation(pi);
        }


        [AutoExpressionField]
        public override string ToString() => As.Expression(() => TypeName);
    }

    [AutoInit]
    public static class DynamicTypeOperation
    {
        public static readonly ConstructSymbol<DynamicTypeEntity>.Simple Create;
        public static readonly ConstructSymbol<DynamicTypeEntity>.From<DynamicTypeEntity> Clone;
        public static readonly ExecuteSymbol<DynamicTypeEntity> Save;
        public static readonly DeleteSymbol<DynamicTypeEntity> Delete;
    }

    public enum DynamicTypeMessage
    {
        [Description("DynamicType '{0}' successfully saved. Go to DynamicPanel now?")]
        DynamicType0SucessfullySavedGoToDynamicPanelNow,

        [Description("Server restarted with errors in dynamic code. Fix errors and restart again.")]
        ServerRestartedWithErrorsInDynamicCodeFixErrorsAndRestartAgain,

        [Description("Remove Save Operation?")]
        RemoveSaveOperation,

        TheEntityShouldBeSynchronizedToApplyMixins,
    }

    public class DynamicTypePrimaryKeyDefinition
    {
        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name;

        [JsonProperty(PropertyName = "type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type;

        [JsonProperty(PropertyName = "identity", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Identity;
    }

    public class DynamicTypeTicksDefinition
    {
        [JsonProperty(PropertyName = "hasTicks", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool HasTicks;

        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name;

        [JsonProperty(PropertyName = "type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type;
    }

    public class DynamicTypeBackMListDefinition
    {
        //TableNameAttribute
        [JsonProperty(PropertyName = "tableName", NullValueHandling = NullValueHandling.Ignore)]
        public string TableName;

        //PreserveOrderAttribute
        [JsonProperty(PropertyName = "preserveOrder")]
        public bool PreserveOrder;

        [JsonProperty(PropertyName = "orderName", NullValueHandling = NullValueHandling.Ignore)]
        public string OrderName;
        //

        //BackReferenceColumnNameAttribute
        [JsonProperty(PropertyName = "backReferenceName", NullValueHandling = NullValueHandling.Ignore)]
        public string BackReferenceName;
    }

    public class DynamicTypeDefinition
    {
        [JsonProperty(PropertyName = "entityKind", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public EntityKind? EntityKind;

        [JsonProperty(PropertyName = "entityData", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public EntityData? EntityData;

        [JsonProperty(PropertyName = "tableName", NullValueHandling = NullValueHandling.Ignore)]
        public string TableName;

        [JsonProperty(PropertyName = "primaryKey", NullValueHandling = NullValueHandling.Ignore)]
        public DynamicTypePrimaryKeyDefinition PrimaryKey;

        [JsonProperty(PropertyName = "ticks", NullValueHandling = NullValueHandling.Ignore)]
        public DynamicTypeTicksDefinition Ticks;

        [JsonProperty(PropertyName = "properties")]
        public List<DynamicProperty> Properties;

        [JsonProperty(PropertyName = "operationCreate")]
        public OperationConstruct OperationCreate;

        [JsonProperty(PropertyName = "operationSave")]
        public OperationExecute OperationSave;

        [JsonProperty(PropertyName = "operationDelete")]
        public OperationDelete OperationDelete;

        [JsonProperty(PropertyName = "operationClone")]
        public OperationConstructFrom OperationClone;

        [JsonProperty(PropertyName = "customInheritance")]
        public DynamicTypeCustomCode CustomInheritance;

        [JsonProperty(PropertyName = "customEntityMembers")]
        public DynamicTypeCustomCode CustomEntityMembers;

        [JsonProperty(PropertyName = "customStartCode")]
        public DynamicTypeCustomCode CustomStartCode;

        [JsonProperty(PropertyName = "customLogicMembers")]
        public DynamicTypeCustomCode CustomLogicMembers;

        [JsonProperty(PropertyName = "customTypes")]
        public DynamicTypeCustomCode CustomTypes;

        [JsonProperty(PropertyName = "customBeforeSchema")]
        public DynamicTypeCustomCode CustomBeforeSchema;

        [JsonProperty(PropertyName = "queryFields")]
        public List<string> QueryFields;

        [JsonProperty(PropertyName = "multiColumnUniqueIndex")]
        public MultiColumnUniqueIndex MultiColumnUniqueIndex;

        [JsonProperty(PropertyName = "toStringExpression", NullValueHandling = NullValueHandling.Ignore)]
        public string ToStringExpression;
    }

    public class MultiColumnUniqueIndex
    {
        [JsonProperty(PropertyName = "fields")]
        public List<string> Fields;

        [JsonProperty(PropertyName = "where")]
        public string Where;

    }

    public class OperationConstruct
    {
        [JsonProperty(PropertyName = "construct")]
        public string Construct;
    }

    public class OperationExecute
    {
        [JsonProperty(PropertyName = "canExecute")]
        public string CanExecute;

        [JsonProperty(PropertyName = "execute")]
        public string Execute;
    }

    public class OperationDelete
    {
        [JsonProperty(PropertyName = "canDelete")]
        public string CanDelete;

        [JsonProperty(PropertyName = "delete")]
        public string Delete;
    }

    public class OperationConstructFrom
    {
        [JsonProperty(PropertyName = "canConstruct")]
        public string CanConstruct;

        [JsonProperty(PropertyName = "construct")]
        public string Construct;
    }

    public class DynamicTypeCustomCode
    {
        [JsonProperty(PropertyName = "code")]
        public string Code;
    }


    public enum DynamicBaseType
    {
        Entity,
        MixinEntity,
        EmbeddedEntity,
        ModelEntity,
    }

    public class DynamicProperty
    {
        [JsonProperty(PropertyName = "uid")]
        public string UID;

        [JsonProperty(PropertyName = "name")]
        public string Name;

        [JsonProperty(PropertyName = "columnName", NullValueHandling = NullValueHandling.Ignore)]
        public string ColumnName;

        [JsonProperty(PropertyName = "type")]
        public string Type;

        [JsonProperty(PropertyName = "columnType", NullValueHandling = NullValueHandling.Ignore)]
        public string ColumnType;

        [JsonProperty(PropertyName = "isNullable")]
        public IsNullable IsNullable;

        [JsonProperty(PropertyName = "uniqueIndex", DefaultValueHandling = DefaultValueHandling.Include)]
        public UniqueIndex UniqueIndex;

        [JsonProperty(PropertyName = "isLite", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsLite;

        [JsonProperty(PropertyName = "isMList", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DynamicTypeBackMListDefinition IsMList;

        [JsonProperty(PropertyName = "size", NullValueHandling = NullValueHandling.Ignore)]
        public int? Size;

        [JsonProperty(PropertyName = "scale", NullValueHandling = NullValueHandling.Ignore)]
        public int? Scale;

        [JsonProperty(PropertyName = "unit", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Unit;

        [JsonProperty(PropertyName = "format", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Format;

        [JsonProperty(PropertyName = "notifyChanges", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool NotifyChanges;

        [JsonProperty(PropertyName = "validators", NullValueHandling = NullValueHandling.Ignore)]
        public List<DynamicValidator> Validators;

        [JsonProperty(PropertyName = "customFieldAttributes", NullValueHandling = NullValueHandling.Ignore)]
        public string CustomFieldAttributes;

        [JsonProperty(PropertyName = "customPropertyAttributes", NullValueHandling = NullValueHandling.Ignore)]
        public string CustomPropertyAttributes;
    }


    public enum IsNullable
    {
        Yes,
        OnlyInMemory,
        No,
    }

    public enum UniqueIndex
    {
        No,
        Yes,
        YesAllowNull,
    }


    class DynamicValidatorConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(DynamicValidator));
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            var type = DynamicValidator.GetDynamicValidatorType(obj.Property("type")!.Value.Value<string>());

            object target = Activator.CreateInstance(type)!;
            serializer.Populate(obj.CreateReader(), target);
            return target;
        }

        public override bool CanWrite { get { return false; } }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    [JsonConverter(typeof(DynamicValidatorConverter))]
    public class DynamicValidator
    {
        [JsonProperty(PropertyName = "type")]
        public string Type;

        public static Type GetDynamicValidatorType(string type)
        {
            switch (type)
            {
                case "NotNull": return typeof(NotNull);
                case "StringLength": return typeof(StringLength);
                case "Decimals": return typeof(Decimals);
                case "NumberIs": return typeof(NumberIs);
                case "CountIs": return typeof(CountIs);
                case "NumberBetween": return typeof(NumberBetween);
                case "DateTimePrecision": return typeof(DateTimePrecision);
                case "TimeSpanPrecision": return typeof(TimeSpanPrecision);
                case "StringCase": return typeof(StringCase);
                default: return typeof(DynamicValidator);
            }
        }

        public virtual string? ExtraArguments()
        {
            return null;
        }

        string Value(object obj)
        {
            if (obj is decimal)
                obj = (double)(decimal)obj;

            return CSharpRenderer.Value(obj);
        }

        public class NotNull : DynamicValidator
        {
            [JsonProperty(PropertyName = "disabled", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool Disabled;

            public override string? ExtraArguments()
            {
                return new string?[]
                {
                    Disabled ? "Disabled=true" : null,
                }.NotNull().ToString(", ");
            }
        }

        public class StringLength : DynamicValidator
        {
            [JsonProperty(PropertyName = "multiLine", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool MultiLine;

            [JsonProperty(PropertyName = "min", NullValueHandling = NullValueHandling.Ignore)]
            public int? Min;

            [JsonProperty(PropertyName = "max", NullValueHandling = NullValueHandling.Ignore)]
            public int? Max;

            [JsonProperty(PropertyName = "allowLeadingSpaces", NullValueHandling = NullValueHandling.Ignore)]
            public bool? AllowLeadingSpaces;

            [JsonProperty(PropertyName = "allowTrailingSpaces", NullValueHandling = NullValueHandling.Ignore)]
            public bool? AllowTrailingSpaces;

            public override string? ExtraArguments()
            {
                return new string?[] 
                {
                    MultiLine ? "MultiLine=true" : null,
                    Min.HasValue ? "Min=" + Value(Min.Value) : null,
                    Max.HasValue ? "Max=" + Value(Max.Value) : null,
                    AllowLeadingSpaces.HasValue ? "AllowLeadingSpaces=" + Value(AllowLeadingSpaces.Value) : null,
                    AllowTrailingSpaces.HasValue ? "AllowTrailingSpaces=" + Value(AllowTrailingSpaces.Value) : null,
                }.NotNull().ToString(", ");
            }
        }

        public class Decimals : DynamicValidator
        {
            [JsonProperty(PropertyName = "decimalPlaces")]
            public int DecimalPlaces;

            public override string? ExtraArguments()
            {
                return Value(DecimalPlaces);
            }
        }

        public class NumberIs : DynamicValidator
        {
            [JsonProperty(PropertyName = "comparisonType")]
            public ComparisonType ComparisonType;

            [JsonProperty(PropertyName = "number")]
            public decimal Number;

            public override string? ExtraArguments()
            {
                return Value(ComparisonType) + ", " + Value(Number);
            }
        }

        public class CountIs : DynamicValidator
        {
            [JsonProperty(PropertyName = "comparisonType")]
            public ComparisonType ComparisonType;

            [JsonProperty(PropertyName = "number")]
            public decimal Number;

            public override string? ExtraArguments()
            {
                return Value(ComparisonType) + ", " + Value(Number);
            }
        }

        public class NumberBetween : DynamicValidator
        {
            [JsonProperty(PropertyName = "min")]
            public decimal Min;

            [JsonProperty(PropertyName = "max")]
            public decimal Max;

            public override string? ExtraArguments()
            {
                return Value(Min) + ", " + Value(Max);
            }
        }

        public class DateTimePrecision : DynamicValidator
        {
            [JsonProperty(PropertyName = "precision")]
            public Signum.Utilities.DateTimePrecision Precision;

            public override string? ExtraArguments()
            {
                return Value(Precision);
            }
        }

        public class TimeSpanPrecision : DynamicValidator
        {
            [JsonProperty(PropertyName = "precision")]
            public Signum.Utilities.DateTimePrecision Precision;

            public override string? ExtraArguments()
            {
                return Value(Precision);
            }
        }

        public class StringCase : DynamicValidator
        {
            [JsonProperty(PropertyName = "textCase")]
            public Signum.Entities.StringCase TextCase;

            public override string? ExtraArguments()
            {
                return Value(TextCase);
            }
        }
    }
}
