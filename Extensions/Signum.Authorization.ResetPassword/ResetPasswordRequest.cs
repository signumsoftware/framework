using Signum.Authorization;
using System.ComponentModel;

namespace Signum.Authorization.ResetPassword;

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class ResetPasswordRequestEntity : Entity
{
    [UniqueIndex(AvoidAttachToUniqueIndexes = true)]
    public string Code { get; set; }
    
    public UserEntity User { get; set; }

    public DateTime RequestDate { get; set; }

    public bool Used { get; set; }

    private static Expression<Func<ResetPasswordRequestEntity, bool>> IsValidExpression = r => 
        !r.Used && !r.IsExpired;

    [ExpressionField(nameof(IsValidExpression))]
    public bool IsValid => IsValidExpression.Evaluate(this);

    private static Expression<Func<ResetPasswordRequestEntity, bool>> IsExpiredExpression = r => 
        Clock.Now >= r.RequestDate.AddHours(2);

    [ExpressionField(nameof(IsExpiredExpression))]
    public bool IsExpired => IsExpiredExpression.Evaluate(this);

    public string? Validate()
    {
        if (this.IsValid)
            return null;

        if (this.Used)
            return ResetPasswordMessage.TheCodeOfYourLinkHasAlreadyBeenUsed.NiceToString();

        return ResetPasswordMessage.YourResetPasswordRequestHasExpired.NiceToString();
    }
}

[AutoInit]
public static class ResetPasswordRequestOperation
{
    public static readonly ExecuteSymbol<ResetPasswordRequestEntity> Execute;
}


public enum ResetPasswordMessage
{
    [Description("You recently requested a new password")]
    YouRecentlyRequestedANewPassword,
    [Description("Your username is:")]
    YourUsernameIs,
    [Description("You can reset your password by following the link below")]
    YouCanResetYourPasswordByFollowingTheLinkBelow,
    [Description("Reset password request")]
    ResetPasswordRequestSubject,
    [Description("Your reset password request has expired")]
    YourResetPasswordRequestHasExpired,
    [Description("We have send you an email to reset your password")]
    WeHaveSendYouAnEmailToResetYourPassword,
    [Description("Email not found")]
    EmailNotFound,
    [Description("Your account has been locked due to several failed logins")]
    YourAccountHasBeenLockedDueToSeveralFailedLogins,
    [Description("Your account has been locked")]
    YourAccountHasBeenLocked,
    TheCodeOfYourLinkIsIncorrect,
    TheCodeOfYourLinkHasAlreadyBeenUsed,
    IfEmailIsValidWeWillSendYouAnEmailToResetYourPassword,
}
