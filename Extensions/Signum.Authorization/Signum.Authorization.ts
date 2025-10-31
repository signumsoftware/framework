//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Security from '../../Signum/React/Signum.Security'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Rules from './Rules/Signum.Authorization.Rules'

export interface UserEntity {
    newPassword: string;
}

export namespace AuthMessage {
  export const NotAuthorizedTo0The1WithId2: MessageKey = new MessageKey("AuthMessage", "NotAuthorizedTo0The1WithId2");
  export const NotAuthorizedToRetrieve0: MessageKey = new MessageKey("AuthMessage", "NotAuthorizedToRetrieve0");
  export const NotAuthorizedTo01: MessageKey = new MessageKey("AuthMessage", "NotAuthorizedTo01");
  export const OnlyActive: MessageKey = new MessageKey("AuthMessage", "OnlyActive");
  export const IncludeTrivialMerges: MessageKey = new MessageKey("AuthMessage", "IncludeTrivialMerges");
  export const DefaultAuthorization: MessageKey = new MessageKey("AuthMessage", "DefaultAuthorization");
  export const MaximumOfThe0: MessageKey = new MessageKey("AuthMessage", "MaximumOfThe0");
  export const MinumumOfThe0: MessageKey = new MessageKey("AuthMessage", "MinumumOfThe0");
  export const SameAs0: MessageKey = new MessageKey("AuthMessage", "SameAs0");
  export const Nothing: MessageKey = new MessageKey("AuthMessage", "Nothing");
  export const Everything: MessageKey = new MessageKey("AuthMessage", "Everything");
  export const UnableToDetermineIfYouCanRead0: MessageKey = new MessageKey("AuthMessage", "UnableToDetermineIfYouCanRead0");
  export const TheQueryDoesNotEnsureThatYouCanRead0: MessageKey = new MessageKey("AuthMessage", "TheQueryDoesNotEnsureThatYouCanRead0");
}

export namespace LoginAuthMessage {
  export const ThePasswordMustHaveAtLeast0Characters: MessageKey = new MessageKey("LoginAuthMessage", "ThePasswordMustHaveAtLeast0Characters");
  export const NotUserLogged: MessageKey = new MessageKey("LoginAuthMessage", "NotUserLogged");
  export const Username0IsNotValid: MessageKey = new MessageKey("LoginAuthMessage", "Username0IsNotValid");
  export const User0IsDeactivated: MessageKey = new MessageKey("LoginAuthMessage", "User0IsDeactivated");
  export const IncorrectPassword: MessageKey = new MessageKey("LoginAuthMessage", "IncorrectPassword");
  export const Login: MessageKey = new MessageKey("LoginAuthMessage", "Login");
  export const MyProfile: MessageKey = new MessageKey("LoginAuthMessage", "MyProfile");
  export const Password: MessageKey = new MessageKey("LoginAuthMessage", "Password");
  export const ChangePassword: MessageKey = new MessageKey("LoginAuthMessage", "ChangePassword");
  export const SwitchUser: MessageKey = new MessageKey("LoginAuthMessage", "SwitchUser");
  export const Logout: MessageKey = new MessageKey("LoginAuthMessage", "Logout");
  export const EnterYourUserNameAndPassword: MessageKey = new MessageKey("LoginAuthMessage", "EnterYourUserNameAndPassword");
  export const Username: MessageKey = new MessageKey("LoginAuthMessage", "Username");
  export const EMailAddress: MessageKey = new MessageKey("LoginAuthMessage", "EMailAddress");
  export const EmailAddressOrUsername: MessageKey = new MessageKey("LoginAuthMessage", "EmailAddressOrUsername");
  export const RememberMe: MessageKey = new MessageKey("LoginAuthMessage", "RememberMe");
  export const IHaveForgottenMyPassword: MessageKey = new MessageKey("LoginAuthMessage", "IHaveForgottenMyPassword");
  export const ShowLoginForm: MessageKey = new MessageKey("LoginAuthMessage", "ShowLoginForm");
  export const LoginWithWindowsUser: MessageKey = new MessageKey("LoginAuthMessage", "LoginWithWindowsUser");
  export const NoWindowsUserFound: MessageKey = new MessageKey("LoginAuthMessage", "NoWindowsUserFound");
  export const LooksLikeYourWindowsUserIsNotAllowedToUseThisApplication: MessageKey = new MessageKey("LoginAuthMessage", "LooksLikeYourWindowsUserIsNotAllowedToUseThisApplication");
  export const IForgotMyPassword: MessageKey = new MessageKey("LoginAuthMessage", "IForgotMyPassword");
  export const EnterYourUserEmail: MessageKey = new MessageKey("LoginAuthMessage", "EnterYourUserEmail");
  export const SendEmail: MessageKey = new MessageKey("LoginAuthMessage", "SendEmail");
  export const GiveUsYourUserEmailToResetYourPassword: MessageKey = new MessageKey("LoginAuthMessage", "GiveUsYourUserEmailToResetYourPassword");
  export const RequestAccepted: MessageKey = new MessageKey("LoginAuthMessage", "RequestAccepted");
  export const PasswordMustHaveAValue: MessageKey = new MessageKey("LoginAuthMessage", "PasswordMustHaveAValue");
  export const PasswordsAreDifferent: MessageKey = new MessageKey("LoginAuthMessage", "PasswordsAreDifferent");
  export const PasswordChanged: MessageKey = new MessageKey("LoginAuthMessage", "PasswordChanged");
  export const PasswordHasBeenChangedSuccessfully: MessageKey = new MessageKey("LoginAuthMessage", "PasswordHasBeenChangedSuccessfully");
  export const NewPassword: MessageKey = new MessageKey("LoginAuthMessage", "NewPassword");
  export const EnterTheNewPassword: MessageKey = new MessageKey("LoginAuthMessage", "EnterTheNewPassword");
  export const ConfirmNewPassword: MessageKey = new MessageKey("LoginAuthMessage", "ConfirmNewPassword");
  export const EnterActualPasswordAndNewOne: MessageKey = new MessageKey("LoginAuthMessage", "EnterActualPasswordAndNewOne");
  export const CurrentPassword: MessageKey = new MessageKey("LoginAuthMessage", "CurrentPassword");
  export const WeHaveSentYouAnEmailToResetYourPassword: MessageKey = new MessageKey("LoginAuthMessage", "WeHaveSentYouAnEmailToResetYourPassword");
  export const UserNameMustHaveAValue: MessageKey = new MessageKey("LoginAuthMessage", "UserNameMustHaveAValue");
  export const InvalidUsernameOrPassword: MessageKey = new MessageKey("LoginAuthMessage", "InvalidUsernameOrPassword");
  export const InvalidUsername: MessageKey = new MessageKey("LoginAuthMessage", "InvalidUsername");
  export const InvalidPassword: MessageKey = new MessageKey("LoginAuthMessage", "InvalidPassword");
  export const AnErrorOccurredRequestNotProcessed: MessageKey = new MessageKey("LoginAuthMessage", "AnErrorOccurredRequestNotProcessed");
  export const TheUserIsNotLongerInTheDatabase: MessageKey = new MessageKey("LoginAuthMessage", "TheUserIsNotLongerInTheDatabase");
  export const Register0: MessageKey = new MessageKey("LoginAuthMessage", "Register0");
  export const Success: MessageKey = new MessageKey("LoginAuthMessage", "Success");
  export const _0HasBeenSucessfullyAssociatedWithUser1InThisDevice: MessageKey = new MessageKey("LoginAuthMessage", "_0HasBeenSucessfullyAssociatedWithUser1InThisDevice");
  export const TryToLogInWithIt: MessageKey = new MessageKey("LoginAuthMessage", "TryToLogInWithIt");
  export const LoginWith0: MessageKey = new MessageKey("LoginAuthMessage", "LoginWith0");
  export const SignInWithMicrosoft: MessageKey = new MessageKey("LoginAuthMessage", "SignInWithMicrosoft");
  export const InvalidTokenDate0: MessageKey = new MessageKey("LoginAuthMessage", "InvalidTokenDate0");
  export const SignUpWithAzureB2C: MessageKey = new MessageKey("LoginAuthMessage", "SignUpWithAzureB2C");
  export const SignInWithAzureB2C: MessageKey = new MessageKey("LoginAuthMessage", "SignInWithAzureB2C");
  export const LoginWithAzureB2C: MessageKey = new MessageKey("LoginAuthMessage", "LoginWithAzureB2C");
}

export const MergeStrategy: EnumType<MergeStrategy> = new EnumType<MergeStrategy>("MergeStrategy");
export type MergeStrategy =
  "Union" |
  "Intersection";

export namespace ResetPasswordB2CMessage {
  export const ResetPasswordRequested: MessageKey = new MessageKey("ResetPasswordB2CMessage", "ResetPasswordRequested");
  export const DoYouWantToContinue: MessageKey = new MessageKey("ResetPasswordB2CMessage", "DoYouWantToContinue");
  export const ResetPassword: MessageKey = new MessageKey("ResetPasswordB2CMessage", "ResetPassword");
}

export const RoleEntity: Type<RoleEntity> = new Type<RoleEntity>("Role");
export interface RoleEntity extends Entities.Entity {
  Type: "Role";
  name: string;
  mergeStrategy: MergeStrategy;
  isTrivialMerge: boolean;
  inheritsFrom: Entities.MList<Entities.Lite<RoleEntity>>;
  description: string | null;
}

export namespace RoleOperation {
  export const Save : Operations.ExecuteSymbol<RoleEntity> = registerSymbol("Operation", "RoleOperation.Save");
  export const Delete : Operations.DeleteSymbol<RoleEntity> = registerSymbol("Operation", "RoleOperation.Delete");
}

export const UserEntity: Type<UserEntity> = new Type<UserEntity>("User");
export interface UserEntity extends Entities.Entity, Basics.IEmailOwnerEntity, Security.IUserEntity {
  Type: "User";
  userName: string;
  passwordHash: string /*Byte[]*/ | null;
  role: Entities.Lite<RoleEntity>;
  email: string | null;
  cultureInfo: Basics.CultureInfoEntity | null;
  disabledOn: string /*DateTime*/ | null;
  state: UserState;
  loginFailedCounter: number;
}

export const UserLiteModel: Type<UserLiteModel> = new Type<UserLiteModel>("UserLiteModel");
export interface UserLiteModel extends Entities.ModelEntity {
  Type: "UserLiteModel";
  userName: string;
  toStringValue: string | null;
  oID: string /*Guid*/ | null;
  sID: string | null;
  photoSuffix: string | null;
}

export namespace UserMessage {
  export const UserIsNotActive: MessageKey = new MessageKey("UserMessage", "UserIsNotActive");
}

export namespace UserOperation {
  export const Create : Operations.ConstructSymbol_Simple<UserEntity> = registerSymbol("Operation", "UserOperation.Create");
  export const Save : Operations.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.Save");
  export const Reactivate : Operations.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.Reactivate");
  export const Deactivate : Operations.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.Deactivate");
  export const AutoDeactivate : Operations.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.AutoDeactivate");
  export const SetPassword : Operations.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.SetPassword");
  export const Delete : Operations.DeleteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.Delete");
}

export const UserState: EnumType<UserState> = new EnumType<UserState>("UserState");
export type UserState =
  "New" |
  "Active" |
  "Deactivated" |
  "AutoDeactivate";

export namespace UserTypeCondition {
  export const DeactivatedUsers : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "UserTypeCondition.DeactivatedUsers");
}

