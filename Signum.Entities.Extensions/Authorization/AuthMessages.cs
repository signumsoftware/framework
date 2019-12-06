using System.ComponentModel;

namespace Signum.Entities.Authorization
{
    public enum AuthMessage
    {
        [Description(@"{0} cycles have been found in the graph of Roles due to the relationships:")]
        _0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships,
        [Description("{0} rules for {1}")]
        _0RulesFor1,
        [Description("Add condition")]
        AuthAdmin_AddCondition,
        [Description("Choose a condition")]
        AuthAdmin_ChooseACondition,
        [Description("Remove condition")]
        AuthAdmin_RemoveCondition,
        AuthorizationCacheSuccessfullyUpdated,
        ChangePassword,
        [Description("Current password")]
        ChangePasswordAspx_ActualPassword,
        [Description("Change password")]
        ChangePasswordAspx_ChangePassword,
        [Description("Confirm new password")]
        ChangePasswordAspx_ConfirmNewPassword,
        [Description("New password")]
        ChangePasswordAspx_NewPassword,
        [Description("Enter your current password and the new one")]
        ChangePasswordAspx_EnterActualPasswordAndNewOne,
        ConfirmNewPassword,
        [Description("The email must have a value")]
        EmailMustHaveAValue,
        EmailSent,
        Email,
        EnterTheNewPassword,
        [Description("Entity Group")]
        EntityGroupsAscx_EntityGroup,
        [Description("Overriden")]
        EntityGroupsAscx_Overriden,
        [Description("Expected a user logged")]
        ExpectedUserLogged,
        ExpiredPassword,
        [Description("Your password has expired. You should change it")]
        ExpiredPasswordMessage,
        [Description("Forgot your password? Enter your login email below.")]
        ForgotYourPasswordEnterYourPasswordBelow,
        [Description("We will send you an email with a link to reset your password.")]
        WeWillSendYouAnEmailWithALinkToResetYourPassword,
        IHaveForgottenMyPassword,
        IncorrectPassword,
        [Description("Enter your username and password")]
        EnterYourUserNameAndPassword,
        InvalidUsernameOrPassword,
        InvalidUsername,
        InvalidPassword,
        [Description("New:")]
        Login_New,
        [Description("Password:")]
        Login_Password,
        [Description("Repeat:")]
        Login_Repeat,
        [Description("Username:")]
        Login_UserName,
        [Description("user")]
        Login_UserName_Watermark,
        [Description("Login")]
        Login,
        [Description("Logout")]
        Logout,
        SwitchUser,
        NewPassword,
        [Description("Not allowed to save this {0} while offline")]
        NotAllowedToSaveThis0WhileOffline,
        [Description("Not authorized to {0} the {1} with Id {2}")]
        NotAuthorizedTo0The1WithId2,
        [Description("Not authorized to Retrieve '{0}'")]
        NotAuthorizedToRetrieve0,
        [Description("Not authorized to Save '{0}'")]
        NotAuthorizedToSave0,
        [Description("Not authorized to change property '{0}' on {1}")]
        NotAuthorizedToChangeProperty0on1,
        NotUserLogged,
        Password,
        PasswordChanged,
        [Description("The given password doesn't match the current one")]
        PasswordDoesNotMatchCurrent,
        [Description("The password has been changed successfully")]
        PasswordHasBeenChangedSuccessfully,
        [Description("The password must have a value")]
        PasswordMustHaveAValue,
        [Description("An error occurred, request not processed.")]
        AnErrorOccurredRequestNotProcessed,
        [Description("We have sent you an email with a link that will allow you to reset your password.")]
        WeHaveSentYouAnEmailToResetYourPassword,
        EnterYourUserEmail,
        RequestAccepted,

        YourPasswordIsNearExpiration,
        PasswordsAreDifferent,
        PasswordsDoNotMatch,

        [Description("Please, {0} into your account")]
        Please0IntoYourAccount,
        [Description("Please, enter your chosen new password")]
        PleaseEnterYourChosenNewPassword,

        Remember,
        RememberMe,
        [Description("Reset Password")]
        ResetPassword,
        ResetPasswordCode,
        [Description("A confirmation code to reset your password has been sent to the email account {0}")]
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

        Username,
        [Description("Username {0} is not valid")]
        Username0IsNotValid,
        [Description("The user name must have a value")]
        UserNameMustHaveAValue,
        View,
        [Description("We received a request to create an account. You can create it following the link below:")]
        WeReceivedARequestToCreateAnAccountYouCanCreateItFollowingTheLinkBelow,
        [Description("You must repeat the new password")]
        YouMustRepeatTheNewPassword,
        [Description("User {0} is disabled")]
        User0IsDisabled,
        SendEmail,
        [Description("Welcome {0}")]
        Welcome0,
        LoginWithAnotherUser,
        TheUserIsNotLongerInTheDatabase,
        [Description("I forgot my password")]
        IForgotMyPassword,
        [Description("Give us your user's email and we will send you an email so you can reset your password.")]
        GiveUsYourUserEmailToResetYourPassword,
        [Description("Login with Windows user")]
        LoginWithWindowsUser,
        [Description("No Windows user found")]
        NoWindowsUserFound,
        [Description("Looks like you windows user is not allowed to use this application, the browser is not providing identity information, or the server is not properly configured.")]
        LooksLikeYourWindowsUserIsNotAllowedToUseThisApplication,
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
    }
}
