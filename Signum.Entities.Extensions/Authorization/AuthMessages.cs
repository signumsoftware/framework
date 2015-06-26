using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

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
        [Description("Write your current password and the new one")]
        ChangePasswordAspx_WriteActualPasswordAndNewOne,
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
        [Description("Introduce your username and password")]
        IntroduceYourUserNameAndPassword,
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
        [Description("The password must have between 7 and 15 characters, each of them being a number 0-9 or a letter")]
        ThePasswordMustHaveBetween7And15CharactersEachOfThemBeingANumber09OrALetter,
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
        ResetPasswordRequestSubject
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

        Overriden
    }

}
