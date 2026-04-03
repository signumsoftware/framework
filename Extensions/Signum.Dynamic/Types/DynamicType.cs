using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.Dynamic.Types;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class DynamicTypeEntity : Entity
{
    public DynamicBaseType BaseType { set; get; }

    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 100), IdentifierValidator(IdentifierType.PascalAscii)]
    public string TypeName { get; set; }

    [DbType(Size = int.MaxValue)]
    string typeDefinition;
    [StringLengthValidator(Min = 3)]
    public string TypeDefinition
    {
        get { return Get(typeDefinition); }
        set
        {
            if (Set(ref typeDefinition, value))
                definition = null;
        }
    }


    static JsonSerializerOptions settings = new JsonSerializerOptions
    {
        IncludeFields = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
             new JsonStringEnumConverter(),
        }
    };

    [Ignore]
    DynamicTypeDefinition? definition;
    public DynamicTypeDefinition GetDefinition()
    {
        return definition ?? (definition = JsonSerializer.Deserialize<DynamicTypeDefinition>(TypeDefinition, settings))!;
    }

    public void SetDefinition(DynamicTypeDefinition definition)
    {
        TypeDefinition = JsonSerializer.Serialize(definition, settings);
        this.definition = definition;
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(TypeDefinition))
        {
            var def = GetDefinition();

            return def.Properties
                .Where(p => p.Name.HasText() && !IdentifierValidatorAttribute.PascalAscii.IsMatch(p.Name))
                .Select(p => ValidationMessage._0DoesNotHaveAValid1IdentifierFormat.NiceToString(p.Name, IdentifierType.PascalAscii))
                .ToString("\n")
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
    TypeSaved,

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
    public string? Name;
    public string? Type;
    public bool Identity;
}

public class DynamicTypeTicksDefinition
{
    public bool HasTicks;
    public string? Name;
    public string? Type;
}

public class DynamicTypeBackMListDefinition
{
    public string? TableName;

    public bool PreserveOrder;

    public string? OrderName;

    public string? BackReferenceName;
}

public class DynamicTypeDefinition
{

    public EntityKind? EntityKind;

    public EntityData? EntityData;

    public string? TableName;

    public DynamicTypePrimaryKeyDefinition? PrimaryKey;

    public DynamicTypeTicksDefinition? Ticks;

    public List<DynamicProperty> Properties;

    public OperationConstruct? OperationCreate;

    public OperationExecute? OperationSave;

    public OperationDelete? OperationDelete;

    public OperationConstructFrom? OperationClone;

    public DynamicTypeCustomCode? CustomInheritance;

    public DynamicTypeCustomCode? CustomEntityMembers;

    public DynamicTypeCustomCode? CustomStartCode;

    public DynamicTypeCustomCode? CustomLogicMembers;

    public DynamicTypeCustomCode? CustomTypes;

    public DynamicTypeCustomCode? CustomBeforeSchema;

    public List<string> QueryFields;

    public MultiColumnUniqueIndex? MultiColumnUniqueIndex;

    public string? ToStringExpression;
}

public class MultiColumnUniqueIndex
{
    public List<string> Fields;

    public string? Where;

}

public class OperationConstruct
{
    public string Construct;
}

public class OperationExecute
{
    public string? CanExecute;
    public string Execute;
}

public class OperationDelete
{
    public string? CanDelete;
    public string Delete;
}

public class OperationConstructFrom
{
    public string? CanConstruct;
    public string Construct;
}

public class DynamicTypeCustomCode
{
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
    public string UID;

    public string Name;

    public string? ColumnName;

    public string Type;

    public string? ColumnType;

    public IsNullable IsNullable;

    public UniqueIndex UniqueIndex;

    public bool? IsLite;

    public DynamicTypeBackMListDefinition IsMList;

    public int? Size;

    public int? Scale;

    public string? Unit;

    public string? Format;

    public bool? NotifyChanges;

    public List<DynamicValidator>? Validators;

    public string? CustomFieldAttributes;

    public string? CustomPropertyAttributes;
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


class DynamicValidatorConverter : JsonConverter<DynamicValidator>
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DynamicValidator);
    }

    public override DynamicValidator? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            var typeName = doc.RootElement.GetProperty("type").GetString()!;
            var type = DynamicValidator.GetDynamicValidatorType(typeName);

            return (DynamicValidator)doc.RootElement.ToObject(type, options);
        }
    }

    public override void Write(Utf8JsonWriter writer, DynamicValidator value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

}

[JsonConverter(typeof(DynamicValidatorConverter))]
public class DynamicValidator
{
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
            default: return typeof(DefaultDynamicValidator);
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
        public bool MultiLine;

        public int? Min;

        public int? Max;

        public bool? AllowLeadingSpaces;

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
        public int DecimalPlaces;

        public override string? ExtraArguments()
        {
            return Value(DecimalPlaces);
        }
    }

    public class NumberIs : DynamicValidator
    {
        public ComparisonType ComparisonType;

        public decimal Number;

        public override string? ExtraArguments()
        {
            return Value(ComparisonType) + ", " + Value(Number);
        }
    }

    public class CountIs : DynamicValidator
    {
        public ComparisonType ComparisonType;

        public decimal Number;

        public override string? ExtraArguments()
        {
            return Value(ComparisonType) + ", " + Value(Number);
        }
    }

    public class NumberBetween : DynamicValidator
    {
        public decimal Min;

        public decimal Max;

        public override string? ExtraArguments()
        {
            return Value(Min) + ", " + Value(Max);
        }
    }

    public class DateTimePrecision : DynamicValidator
    {
        public Utilities.DateTimePrecision Precision;

        public override string? ExtraArguments()
        {
            return Value(Precision);
        }
    }

    public class TimeSpanPrecision : DynamicValidator
    {
        public Utilities.DateTimePrecision Precision;

        public override string? ExtraArguments()
        {
            return Value(Precision);
        }
    }

    public class StringCase : DynamicValidator
    {
        public Signum.Entities.Validation.StringCase TextCase;

        public override string? ExtraArguments()
        {
            return Value(TextCase);
        }
    }

    public class DefaultDynamicValidator : DynamicValidator
    {
    }
}
