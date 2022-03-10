using Signum.Entities.Authorization;
using System.ComponentModel;

namespace Signum.Entities.ConcurrentUser;
[EntityKind(EntityKind.System, EntityData.Transactional)]
public class ConcurrentUserEntity : Entity
{
    [ImplementedByAll]
    public Lite<Entity> TargetEntity { get; set; }

    public DateTime StartTime { get; set; }

    public Lite<UserEntity> User { get; set; }

    [StringLengthValidator(Max = 100)]
    public string SignalRConnectionID { get; set; }

    public bool IsModified { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => $"{User} - {StartTime}");
}

[AutoInit]
public static class ConcurrentUserOperation
{
    public static readonly DeleteSymbol<ConcurrentUserEntity> Delete;
}

public enum ConcurrentUserMessage
{
    CurrentlyEditing,

    [Description("Database changes detected!")]
    DatabaseChangesDetected,

    [Description("Looks like someone just saved {0} in the database.")]
    LooksLikeSomeoneJustSaved0ToTheDatabase,

    [Description("Do you want to reload it?")]
    DoYouWantToReloadIt,

    [Description("You have local changes but the entity has been saved in the database... you will not be able to save changes.")]
    YouHaveLocalChangesButTheEntityHasBeenSavedInTheDatabaseYouWillNotBeAbleToSaveChanges,

    [Description("Looks like you are not the only one currently modifying {0}... only the first one will be able to save changes!")]
    LooksLikeYouAreNotTheOnlyOneCurrentlyModifiying0OnlyTheFirstOneWillBeAbleToSaveChanges,
    

    [Description("WARNING: You will lost your current changes.")]
    WarningYouWillLostYourCurrentChanges,

    [Description("Consider opening {0} in a new tab and apply your changes manually")]
    ConsiderOpening0InANewTabAndApplyYourChangesManually,
}
