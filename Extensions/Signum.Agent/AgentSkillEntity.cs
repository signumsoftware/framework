namespace Signum.Agent;

public class AgentUseCaseSymbol : Symbol
{
    private AgentUseCaseSymbol() { }
}

[AutoInit]
public static class AgentUseCase
{
    public static AgentUseCaseSymbol DefaultChatbot = null!;
    public static AgentUseCaseSymbol Summarizer = null!;
}

[EntityKind(EntityKind.Main, EntityData.Master)]
public class AgentSkillEntity : Entity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 1, Max = 200)]
    public string Name { get; set; }

    [StringLengthValidator(Min = 1, Max = 200)]
    public string SkillCode { get; set; }

    public bool Active { get; set; } = true;

    public AgentUseCaseSymbol? UseCase { get; set; }

    [StringLengthValidator(Min = 1, Max = 500)]
    public string? ShortDescription { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string? Instructions { get; set; }

    [BindParent]
    public MList<AgentSkillPropertyOverrideEmbedded> PropertyOverrides { get; set; } = new MList<AgentSkillPropertyOverrideEmbedded>();

    [BindParent]
    public MList<AgentSkillSubSkillEmbedded> SubSkills { get; set; } = new MList<AgentSkillSubSkillEmbedded>();

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(SkillCode) && SkillCode.HasText() && AgentSkillLogic.RegisteredCodes.Any())
        {
            if (!AgentSkillLogic.RegisteredCodes.ContainsKey(SkillCode))
                return $"SkillCode '{SkillCode}' is not registered. Available: {AgentSkillLogic.RegisteredCodes.Keys.ToString(", ")}";
        }

        return base.PropertyValidation(pi);
    }
}

public class AgentSkillPropertyOverrideEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Min = 1, Max = 200)]
    public string PropertyName { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string? Value { get; set; }
}

public class AgentSkillSubSkillEmbedded : EmbeddedEntity
{
    public Lite<AgentSkillEntity> Skill { get; set; }

    public SkillActivation Activation { get; set; }
}

[AutoInit]
public static class AgentSkillOperation
{
    public static ExecuteSymbol<AgentSkillEntity> Save = null!;
    public static DeleteSymbol<AgentSkillEntity> Delete = null!;
}
