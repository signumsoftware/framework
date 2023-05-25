using Signum.Authorization;
using System.ComponentModel;

namespace Signum.ConcurrentUser;

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
    ConcurrentUsers,

    CurrentlyEditing,

    [Description("Database changes detected!")]
    DatabaseChangesDetected,

    [Description("Looks like someone just saved {0} in the database.")]
    LooksLikeSomeoneJustSaved0ToTheDatabase,

    [Description("Do you want to reload it?")]
    DoYouWantToReloadIt,

    [Description("You have local changes in {0} which is currently open by other users. So far no one else has made modifications. ")]
    YouHaveLocalChangesIn0ThatIsCurrentlyOpenByOtherUsersSoFarNoOneElseHasMadeModifications,

    [Description("Looks like you are not the only one currently modifying {0}... only the first one will be able to save changes!")]
    LooksLikeYouAreNotTheOnlyOneCurrentlyModifiying0OnlyTheFirstOneWillBeAbleToSaveChanges,

    [Description("You have local changes but {0} has already been saved in the database... you will not be able to save changes :(")]
    YouHaveLocalChangesBut0HasAlreadyBeenSavedInTheDatabaseYouWillNotBeAbleToSaveChanges,

    [Description("This is not the latest version of {0}")]
    ThisIsNotTheLatestVersionOf0,

    [Description("Reload it!")] 
    ReloadIt,

    [Description("WARNING: You will lost your current changes.")]
    WarningYouWillLostYourCurrentChanges,

    [Description("Consider opening {0} in a new tab and apply your changes manually")]
    ConsiderOpening0InANewTabAndApplyYourChangesManually,
}
