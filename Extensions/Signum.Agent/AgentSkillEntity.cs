namespace Signum.Agent;

// ─── AgentSkillCodeEntity ──────────────────────────────────────────────────────

/// <summary>
/// Catalog of registered AgentSkillCode types. Rows are auto-generated/synchronized
/// from code (same pattern as EmailModelEntity) — never created manually.
/// </summary>
[EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
public class AgentSkillCodeEntity : Entity
{
    [UniqueIndex]
    public string FullClassName { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => FullClassName.AfterLast('.'));
}

// ─── AgentUseCaseSymbol ────────────────────────────────────────────────────────

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

// ─── AgentSkillEntity ─────────────────────────────────────────────────────────

[EntityKind(EntityKind.Main, EntityData.Master)]
public class AgentSkillEntity : Entity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 1, Max = 200)]
    public string Name { get; set; }

    public AgentSkillCodeEntity SkillCode { get; set; }

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

    protected override string? ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
    {
        if (sender is AgentSkillPropertyOverrideEmbedded po
            && pi.Name == nameof(AgentSkillPropertyOverrideEmbedded.Value)
            && SkillCode != null
            && AgentSkillLogic.RegisteredCodes.TryGetValue(SkillCode.FullClassName, out var codeType))
        {
            var propInfo = codeType.GetProperty(po.PropertyName, BindingFlags.Public | BindingFlags.Instance);
            var attr = propInfo?.GetCustomAttribute<AgentSkillPropertyAttribute>();
            if (attr != null)
                return attr.ValidateValue(po.Value, propInfo!.PropertyType);
        }

        return base.ChildPropertyValidation(sender, pi);
    }
}

// ─── AgentSkillPropertyOverrideEmbedded ───────────────────────────────────────

public class AgentSkillPropertyOverrideEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Min = 1, Max = 200)]
    public string PropertyName { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string? Value { get; set; }
}

// ─── AgentSkillSubSkillEmbedded ───────────────────────────────────────────────

public class AgentSkillSubSkillEmbedded : EmbeddedEntity
{
    public Lite<AgentSkillEntity> Skill { get; set; }

    public SkillActivation Activation { get; set; }
}

// ─── AgentSkillOperation ──────────────────────────────────────────────────────

[AutoInit]
public static class AgentSkillOperation
{
    public static ExecuteSymbol<AgentSkillEntity> Save = null!;
    public static DeleteSymbol<AgentSkillEntity> Delete = null!;
}
