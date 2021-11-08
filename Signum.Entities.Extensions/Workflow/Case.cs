using Signum.Entities.Authorization;
using Signum.Entities.Basics;

namespace Signum.Entities.Workflow;

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class CaseEntity : Entity
{
    
    public WorkflowEntity Workflow { get; set; }

    public Lite<CaseEntity>? ParentCase { get; set; }

    [StringLengthValidator(Min = 1, Max = 100)]
    public string Description { get; set; }

    [ImplementedByAll]
    
    public ICaseMainEntity MainEntity { get; set; }

    public DateTime StartDate { get; set; } = TimeZoneManager.Now;
    public DateTime? FinishDate { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Description);
}

[AutoInit]
public static class CaseOperation
{
    public static readonly ExecuteSymbol<CaseEntity> SetTags;
    public static readonly ExecuteSymbol<CaseEntity> Cancel;
    public static readonly ExecuteSymbol<CaseEntity> Reactivate;
}

public interface ICaseMainEntity : IEntity
{

}

public class CaseTagsModel : ModelEntity
{
    [PreserveOrder]
    [NoRepeatValidator]
    public MList<CaseTagTypeEntity> CaseTags { get; set; } = new MList<CaseTagTypeEntity>();

    [PreserveOrder]
    [NoRepeatValidator]
    public MList<CaseTagTypeEntity> OldCaseTags { get; set; } = new MList<CaseTagTypeEntity>();
}


[EntityKind(EntityKind.System, EntityData.Transactional)]
public class CaseTagEntity : Entity
{
    public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

    
    public Lite<CaseEntity> Case { get; set; }

    
    public CaseTagTypeEntity TagType { get; set; }

    [ImplementedBy(typeof(UserEntity))]
    public Lite<IUserEntity> CreatedBy { get; set; }
}

[EntityKind(EntityKind.Main, EntityData.Master)]
public class CaseTagTypeEntity : Entity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 2, Max = 100)]
    public string Name { get; set; }

    [StringLengthValidator(Min = 3, Max = 12)]
    public string Color { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);
}

[AutoInit]
public static class CaseTagTypeOperation
{
    public static readonly ExecuteSymbol<CaseTagTypeEntity> Save;
}

[InTypeScript(true), DescriptionOptions(DescriptionOptions.Members)]
public enum CaseFlowColor
{
    CaseMaxDuration,
    AverageDuration,
    EstimatedDuration
}
