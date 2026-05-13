using Signum.Authorization.Rules;

namespace Signum.Authorization;

[EntityKind(EntityKind.Main, EntityData.Transactional)]
public class UserEntity : Entity, IEmailOwnerEntity, IUserEntity
{
    public static Func<string, UserEntity?, string?> ValidatePassword = (p, user) =>
    {
        if (p.Length >= 5)
            return null;

        return LoginAuthMessage.ThePasswordMustHaveAtLeast0Characters.NiceToString(5);
    };

    public static string? OnValidatePassword(string password, UserEntity? user)
    {
        if (ValidatePassword != null)
            return ValidatePassword(password, user);

        return null;
    }

    [UniqueIndex(AvoidAttachToUniqueIndexes = true)]
    [StringLengthValidator(Min = 2, Max = 100)]
    public string UserName { get; set; }

    [DbType(Size = 128), QueryableProperty(false)]
    public byte[]? PasswordHash { get; set; }

    [Ignore]
    public bool PasswordIsChanging { get; set; }

    public Lite<RoleEntity> Role { get; set; }

    [StringLengthValidator(Max = 200), EMailValidator]
    public string? Email { get; set; }

    public CultureInfoEntity? CultureInfo { get; set; }

    public DateTime? DisabledOn { get; set; }

    public bool MustChangePassword { get; set; }

    public UserState State { get; set; } = UserState.New;

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(State))
        {
            if (DisabledOn != null && State is not (UserState.Deactivated or UserState.AutoDeactivate))
                return AuthAdminMessage.TheUserStateMustBeDisabled.NiceToString();
        }

        if (pi.Name == nameof(PasswordIsChanging) && PasswordIsChanging)
            return AuthAdminMessage.PasswordChangeIsNotCompleted.NiceToString();

        if (pi.Name == nameof(ExternalId) && ExternalId != null && PasswordHash != null && !AllowPasswordForUserWithExternalId)
            return UserExternalIdMessage.TheUser0IsConnectedToAnExternalProviderAndCanNotHaveALocalPasswordSet.NiceToString(this);

        return base.PropertyValidation(pi);
    }

    public int LoginFailedCounter { get; set; }

    [UniqueIndex]
    [StringLengthValidator(Max = 500)]
    public string? ExternalId { get; set; }

    public static bool AllowPasswordForUserWithExternalId = false;

    public static string? CurrentExternalId =>
        UserHolder.Current?.GetClaim("ExternalId") as string;

    public static Lite<UserEntity> Current => (Lite<UserEntity>)UserHolder.Current?.User!;

    [AutoExpressionField]
    public EmailOwnerData EmailOwnerData => As.Expression(() => new EmailOwnerData
    {
        Owner = this.ToLite(),
        CultureInfo = CultureInfo,
        DisplayName = UserName,
        Email = Email,
        ExternalId = null,
    });

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => UserName);
}


public enum UserState
{
    [Ignore]
    New = -1,
    Active,
    Deactivated,
    AutoDeactivate, // manuall reactivate, new button und deactivatead is hidden in ui, über logic cann autodeactivated
}

[AutoInit]
public static class UserOperation
{
    public static ConstructSymbol<UserEntity>.Simple Create;
    public static ExecuteSymbol<UserEntity> Save;
    public static ExecuteSymbol<UserEntity> Reactivate;
    public static ExecuteSymbol<UserEntity> Deactivate;
    public static ExecuteSymbol<UserEntity> AutoDeactivate;
    public static DeleteSymbol<UserEntity> Delete;
}

public class IncorrectUsernameException : ApplicationException
{
    public IncorrectUsernameException() { }
    public IncorrectUsernameException(string message) : base(message) { }
}

public class UserLockedException : ApplicationException
{
    public UserLockedException() { }
    public UserLockedException(string message) : base(message) { }
}

public class IncorrectPasswordException : ApplicationException
{
    public IncorrectPasswordException() { }
    public IncorrectPasswordException(string message) : base(message) { }
}




[AutoInit]
public static class UserTypeCondition
{
    public static readonly TypeConditionSymbol DeactivatedUsers;
}


[AllowUnauthenticated]
public class UserLiteModel : ModelEntity
{
    public string UserName { get; set; }

    public string? ToStringValue { get; set; }

    public string? ExternalId { get; set; }

    public string? PhotoSuffix { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => ToStringValue ?? UserName);
}



public enum UserMessage
{
    UserIsNotActive
}

public enum UserExternalIdMessage
{
    [Description("The user {0} is connected to an external provider and can not have a local password set")]
    TheUser0IsConnectedToAnExternalProviderAndCanNotHaveALocalPasswordSet,
}
