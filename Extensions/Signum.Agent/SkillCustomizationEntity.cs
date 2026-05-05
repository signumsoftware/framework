namespace Signum.Agent;

[EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
public class SkillCodeEntity : Entity
{
    [UniqueIndex]
    public string ClassName { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => ClassName);
}

[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class AgentSymbol : Symbol
{
    private AgentSymbol() { }

    public AgentSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }

    public Lite<SkillCustomizationEntity>? SkillCustomization { get; set; }
}

[AutoInit]
public static class DefaultAgent
{
    public static AgentSymbol Chatbot;
    public static AgentSymbol QuestionSummarizer;
    public static AgentSymbol ConversationSumarizer;
}

[EntityKind(EntityKind.Main, EntityData.Master)]
public class SkillCustomizationEntity : Entity
{
    public SkillCodeEntity SkillCode { get; set; }

    [StringLengthValidator(Min = 1, Max = 500)]
    public string? ShortDescription { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string? Instructions { get; set; }

    [BindParent]
    public MList<SkillPropertyEmbedded> Properties { get; set; } = new MList<SkillPropertyEmbedded>();

    [BindParent]
    public MList<SubSkillEmbedded> SubSkills { get; set; } = new MList<SubSkillEmbedded>();

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => IsNew ? this.BaseToString() : SkillCode.ToString());

    protected override string? ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
    {
        if (sender is SkillPropertyEmbedded po
            && pi.Name == nameof(SkillPropertyEmbedded.Value)
            && SkillCode != null)
        {
            var propInfo = SkillCode.ToType().GetProperty(po.PropertyName, BindingFlags.Public | BindingFlags.Instance);
            if (propInfo == null)
                return $"Skill {SkillCode} has not property {po.PropertyName}";

            var attr = propInfo?.GetCustomAttribute<SkillPropertyAttribute>();
            if (propInfo == null || attr == null)
                return $"Property {po.PropertyName} of type  {SkillCode} has not AgentSkillProperty";
            
            return attr.ValidateValue(po.Value, propInfo!.PropertyType);
        }

        return base.ChildPropertyValidation(sender, pi);
    }
}

public class SkillPropertyEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Min = 1, Max = 200)]
    public string PropertyName { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string? Value { get; set; }
}

public class SubSkillEmbedded : EmbeddedEntity
{
    // Can reference either an AgentSkillEntity (customised) or AgentSkillCodeEntity (default, no DB entity needed)
    [ImplementedBy(typeof(SkillCustomizationEntity), typeof(SkillCodeEntity))]
    public Entity Skill { get; set; }

    public SkillActivation Activation { get; set; }
}

[AutoInit]
public static class SkillCustomizationOperation
{
    public static ExecuteSymbol<SkillCustomizationEntity> Save = null!;
    public static DeleteSymbol<SkillCustomizationEntity> Delete = null!;
    public static ConstructSymbol<SkillCustomizationEntity>.From<AgentSymbol> CreateFromAgent = null!;
}
