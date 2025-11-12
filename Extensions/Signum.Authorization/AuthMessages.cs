namespace Signum.Authorization;

[AllowUnauthenticated]
public enum LoginAuthMessage
{
    [Description("The password must have at least {0} characters")]
    ThePasswordMustHaveAtLeast0Characters,
    NotUserLogged,

    [Description("Username {0} is not valid")]
    Username0IsNotValid,

    [Description("User {0} is deactivated")]
    User0IsDeactivated,

    IncorrectPassword,

    Login,
    MyProfile,
    Password,
    ChangePassword,
    SwitchUser,
    [Description("Logout")]
    Logout,
    EnterYourUserNameAndPassword,
    Username,
    [Description("E-Mail Address")]
    EMailAddress,
    [Description("E-Mail Address or Username")]
    EmailAddressOrUsername,
    RememberMe,
    IHaveForgottenMyPassword,

    [Description("Show login form")]
    ShowLoginForm,



    [Description("I forgot my password")]
    IForgotMyPassword,
    EnterYourUserEmail,
    SendEmail,
    [Description("Give us your user's email and we will send you an email so you can reset your password.")]
    GiveUsYourUserEmailToResetYourPassword,
    RequestAccepted,
    [Description("The password must have a value")]
    PasswordMustHaveAValue,
    PasswordsAreDifferent,
    PasswordChanged,
    [Description("The password has been changed successfully")]
    PasswordHasBeenChangedSuccessfully,
    [Description("New password")]
    NewPassword,
    EnterTheNewPassword,
    ConfirmNewPassword,
    [Description("Enter your current password and the new one")]
    EnterActualPasswordAndNewOne,
    [Description("Current password")]
    CurrentPassword,

    [Description("We have sent you an email with a link that will allow you to reset your password.")]
    WeHaveSentYouAnEmailToResetYourPassword,

    [Description("The user name must have a value")]
    UserNameMustHaveAValue,
    InvalidUsernameOrPassword,
    InvalidUsername,
    InvalidPassword,

    [Description("An error occurred, request not processed.")]
    AnErrorOccurredRequestNotProcessed,

    TheUserIsNotLongerInTheDatabase,

    [Description("Register {0}")]
    Register0,

    [Description("Success")]
    Success,

    [Description("{0} has been successfully associated with user {1} in this device.")]
    _0HasBeenSucessfullyAssociatedWithUser1InThisDevice,

    [Description("Try to log-in with it!")]
    TryToLogInWithIt,

    [Description("Login with {0}")]
    LoginWith0,

    [Description("Sign in with Microsoft")]
    SignInWithMicrosoft,


    [Description("Invalid token date {0}")]
    InvalidTokenDate0,

    [Description("Sign up with Azure B2C")]
    SignUpWithAzureB2C,
    [Description("Sign in with Azure B2C")]
    SignInWithAzureB2C,
    [Description("Login with Azure B2C")]
    LoginWithAzureB2C,
}

[AllowUnauthenticated]
public enum ResetPasswordB2CMessage
{
    [Description("Reset Password requested")]
    ResetPasswordRequested,

    [Description("Do you want to continue?")]
    DoYouWantToContinue,

    [Description("Reset Password")]
    ResetPassword,
}

public enum AuthMessage
{
    [Description("Not authorized to {0} the '{1}' with Id {2}")]
    NotAuthorizedTo0The1WithId2,
    [Description("Not authorized to retrieve '{0}'")]
    NotAuthorizedToRetrieve0,
    [Description("Not authorized to {0} '{1}'")]
    NotAuthorizedTo01,

    OnlyActive,

    IncludeTrivialMerges,

    [Description("Default Authorization: ")]
    DefaultAuthorization,

    [Description("Maximum of the {0}")]
    MaximumOfThe0,
    [Description("Minimum of the {0}")]
    MinumumOfThe0,
    [Description("Same as {0}")]
    SameAs0,
    Nothing,
    Everything,
    [Description("Unable to determine if you can read {0}")]
    UnableToDetermineIfYouCanRead0,

    [Description("The query does not ensure that you can read {0}")]
    TheQueryDoesNotEnsureThatYouCanRead0,
}
