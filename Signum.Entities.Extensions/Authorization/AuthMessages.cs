using System.ComponentModel;

namespace Signum.Entities.Authorization
{
    [AllowUnathenticated]
    public enum LoginAuthMessage
    {
        [Description("The password must have at least {0} characters")]
        ThePasswordMustHaveAtLeast0Characters,
        NotUserLogged,

        [Description("Username {0} is not valid")]
        Username0IsNotValid,

        [Description("User {0} is disabled")]
        User0IsDisabled,

        IncorrectPassword,

        Login,
        Password,
        ChangePassword,
        SwitchUser,
        [Description("Logout")]
        Logout,
        EnterYourUserNameAndPassword,
        Username,
        IHaveForgottenMyPassword,

        [Description("Show login form")]
        ShowLoginForm,

        [Description("Login with Windows user")]
        LoginWithWindowsUser,
        [Description("No Windows user found")]
        NoWindowsUserFound,
        [Description("Looks like you windows user is not allowed to use this application, the browser is not providing identity information, or the server is not properly configured.")]
        LooksLikeYourWindowsUserIsNotAllowedToUseThisApplication,

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

        Remember,
        RememberMe,
        [Description("Reset Password")]
        ResetPassword,
        ResetPasswordCode,
        [Description("A confirmation code to reset your password has been sent")]
        ResetPasswordCodeHasBeenSent,
        [Description("Your password has been successfully changed")]
        ResetPasswordSuccess,
        Save,
        TheConfirmationCodeThatYouHaveJustSentIsInvalid,
        [Description("The password must have at least 5 characters")]
        ThePasswordMustHaveAtLeast5Characters,
        [Description("There has been an error with your request to reset your password. Please, enter your login.")]
        ThereHasBeenAnErrorWithYourRequestToResetYourPasswordPleaseEnterYourLogin,
        [Description("There's not a registered user with that email address")]
        ThereSNotARegisteredUserWithThatEmailAddress,
        [Description("The specified passwords don't match")]
        TheSpecifiedPasswordsDontMatch,
        TheUserStateMustBeDisabled,
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
    }

    public enum AuthMessage
    {
        [Description("Not authorized to {0} the {1} with Id {2}")]
        NotAuthorizedTo0The1WithId2,
        [Description("Not authorized to Retrieve '{0}'")]
        NotAuthorizedToRetrieve0
    }

    public enum AuthEmailMessage
    {
        [Description("You recently requested a new password")]
        YouRecentlyRequestedANewPassword,
        [Description("Your username is:")]
        YourUsernameIs,
        [Description("You can reset your password by following the link below")]
        YouCanResetYourPasswordByFollowingTheLinkBelow,
        [Description("Reset password request")]
        ResetPasswordRequestSubject,
        [Description("Password changed")]
        PasswordChangedSubject,
        [Description("Your password has recently been changed")]
        YourPasswordHasRecentlyBeenChanged,
        [Description("If you have not changed your password, please get in contact with us")]
        IfYouHaveNotChangedYourPasswordPleaseGetInContactWithUs,
        [Description("Your account has been locked")]
        AccountLockedSubject,
        [Description("Your account has been locked due to several failed logins")]
        YourAccountHasBeenBlockedDueToSeveralFailedLogins,
        [Description("Your reset password request has expired")]
        YourResetPasswordRequestHasExpired,
        [Description("We have send you an email to reset your password")]
        WeHaveSendYouAnEmailToResetYourPassword,
        [Description("Email not found")]
        EmailNotFound,
    }

    public enum AuthAdminMessage
    {
        [Description("{0} of {1}")]
        _0of1,
        Nothing,
        Everything,
        TypeRules,
        PermissionRules,

        Allow,
        Deny,

        Overriden,
        NoRoles,
        Filter,
        PleaseSaveChangesFirst,
        ResetChanges,
        SwitchTo,

        [Description("{0} (in UI)")]
        _0InUI,
        [Description("{0} (in DB only)")]
        _0InDB,

        [Description("Can not be modified")]
        CanNotBeModified,

        [Description("Can not be modified because is a {0}")]
        CanNotBeModifiedBecauseIsA0,

        [Description("Can not be modified because is not a {0}")]
        CanNotBeModifiedBecauseIsNotA0,

        [Description("{0} rules for {1}")]
        _0RulesFor1,

        TheUserStateMustBeDisabled,

        [Description(@"{0} cycles have been found in the graph of Roles due to the relationships:")]
        _0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships,

        Save,
    }
}
