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

export module AuthMessage {
  export const NotAuthorizedTo0The1WithId2 = new MessageKey("AuthMessage", "NotAuthorizedTo0The1WithId2");
  export const NotAuthorizedToRetrieve0 = new MessageKey("AuthMessage", "NotAuthorizedToRetrieve0");
  export const OnlyActive = new MessageKey("AuthMessage", "OnlyActive");
  export const IncludeTrivialMerges = new MessageKey("AuthMessage", "IncludeTrivialMerges");
  export const DefaultAuthorization = new MessageKey("AuthMessage", "DefaultAuthorization");
  export const MaximumOfThe0 = new MessageKey("AuthMessage", "MaximumOfThe0");
  export const MinumumOfThe0 = new MessageKey("AuthMessage", "MinumumOfThe0");
  export const SameAs0 = new MessageKey("AuthMessage", "SameAs0");
  export const Nothing = new MessageKey("AuthMessage", "Nothing");
  export const Everything = new MessageKey("AuthMessage", "Everything");
}

export module LoginAuthMessage {
  export const ThePasswordMustHaveAtLeast0Characters = new MessageKey("LoginAuthMessage", "ThePasswordMustHaveAtLeast0Characters");
  export const NotUserLogged = new MessageKey("LoginAuthMessage", "NotUserLogged");
  export const Username0IsNotValid = new MessageKey("LoginAuthMessage", "Username0IsNotValid");
  export const User0IsDeactivated = new MessageKey("LoginAuthMessage", "User0IsDeactivated");
  export const IncorrectPassword = new MessageKey("LoginAuthMessage", "IncorrectPassword");
  export const Login = new MessageKey("LoginAuthMessage", "Login");
  export const MyProfile = new MessageKey("LoginAuthMessage", "MyProfile");
  export const Password = new MessageKey("LoginAuthMessage", "Password");
  export const ChangePassword = new MessageKey("LoginAuthMessage", "ChangePassword");
  export const SwitchUser = new MessageKey("LoginAuthMessage", "SwitchUser");
  export const Logout = new MessageKey("LoginAuthMessage", "Logout");
  export const EnterYourUserNameAndPassword = new MessageKey("LoginAuthMessage", "EnterYourUserNameAndPassword");
  export const Username = new MessageKey("LoginAuthMessage", "Username");
  export const EMailAddress = new MessageKey("LoginAuthMessage", "EMailAddress");
  export const EmailAddressOrUsername = new MessageKey("LoginAuthMessage", "EmailAddressOrUsername");
  export const RememberMe = new MessageKey("LoginAuthMessage", "RememberMe");
  export const IHaveForgottenMyPassword = new MessageKey("LoginAuthMessage", "IHaveForgottenMyPassword");
  export const ShowLoginForm = new MessageKey("LoginAuthMessage", "ShowLoginForm");
  export const LoginWithWindowsUser = new MessageKey("LoginAuthMessage", "LoginWithWindowsUser");
  export const NoWindowsUserFound = new MessageKey("LoginAuthMessage", "NoWindowsUserFound");
  export const LooksLikeYourWindowsUserIsNotAllowedToUseThisApplication = new MessageKey("LoginAuthMessage", "LooksLikeYourWindowsUserIsNotAllowedToUseThisApplication");
  export const IForgotMyPassword = new MessageKey("LoginAuthMessage", "IForgotMyPassword");
  export const EnterYourUserEmail = new MessageKey("LoginAuthMessage", "EnterYourUserEmail");
  export const SendEmail = new MessageKey("LoginAuthMessage", "SendEmail");
  export const GiveUsYourUserEmailToResetYourPassword = new MessageKey("LoginAuthMessage", "GiveUsYourUserEmailToResetYourPassword");
  export const RequestAccepted = new MessageKey("LoginAuthMessage", "RequestAccepted");
  export const PasswordMustHaveAValue = new MessageKey("LoginAuthMessage", "PasswordMustHaveAValue");
  export const PasswordsAreDifferent = new MessageKey("LoginAuthMessage", "PasswordsAreDifferent");
  export const PasswordChanged = new MessageKey("LoginAuthMessage", "PasswordChanged");
  export const PasswordHasBeenChangedSuccessfully = new MessageKey("LoginAuthMessage", "PasswordHasBeenChangedSuccessfully");
  export const NewPassword = new MessageKey("LoginAuthMessage", "NewPassword");
  export const EnterTheNewPassword = new MessageKey("LoginAuthMessage", "EnterTheNewPassword");
  export const ConfirmNewPassword = new MessageKey("LoginAuthMessage", "ConfirmNewPassword");
  export const EnterActualPasswordAndNewOne = new MessageKey("LoginAuthMessage", "EnterActualPasswordAndNewOne");
  export const CurrentPassword = new MessageKey("LoginAuthMessage", "CurrentPassword");
  export const WeHaveSentYouAnEmailToResetYourPassword = new MessageKey("LoginAuthMessage", "WeHaveSentYouAnEmailToResetYourPassword");
  export const UserNameMustHaveAValue = new MessageKey("LoginAuthMessage", "UserNameMustHaveAValue");
  export const InvalidUsernameOrPassword = new MessageKey("LoginAuthMessage", "InvalidUsernameOrPassword");
  export const InvalidUsername = new MessageKey("LoginAuthMessage", "InvalidUsername");
  export const InvalidPassword = new MessageKey("LoginAuthMessage", "InvalidPassword");
  export const AnErrorOccurredRequestNotProcessed = new MessageKey("LoginAuthMessage", "AnErrorOccurredRequestNotProcessed");
  export const TheUserIsNotLongerInTheDatabase = new MessageKey("LoginAuthMessage", "TheUserIsNotLongerInTheDatabase");
  export const Register0 = new MessageKey("LoginAuthMessage", "Register0");
  export const Success = new MessageKey("LoginAuthMessage", "Success");
  export const _0HasBeenSucessfullyAssociatedWithUser1InThisDevice = new MessageKey("LoginAuthMessage", "_0HasBeenSucessfullyAssociatedWithUser1InThisDevice");
  export const TryToLogInWithIt = new MessageKey("LoginAuthMessage", "TryToLogInWithIt");
  export const LoginWith0 = new MessageKey("LoginAuthMessage", "LoginWith0");
  export const SignInWithMicrosoft = new MessageKey("LoginAuthMessage", "SignInWithMicrosoft");
}

export const MergeStrategy = new EnumType<MergeStrategy>("MergeStrategy");
export type MergeStrategy =
  "Union" |
  "Intersection";

export const RoleEntity = new Type<RoleEntity>("Role");
export interface RoleEntity extends Entities.Entity {
  Type: "Role";
  name: string;
  mergeStrategy: MergeStrategy;
  isTrivialMerge: boolean;
  inheritsFrom: Entities.MList<Entities.Lite<RoleEntity>>;
  description: string | null;
}

export module RoleOperation {
  export const Save : Operations.ExecuteSymbol<RoleEntity> = registerSymbol("Operation", "RoleOperation.Save");
  export const Delete : Operations.DeleteSymbol<RoleEntity> = registerSymbol("Operation", "RoleOperation.Delete");
}

export const UserEntity = new Type<UserEntity>("User");
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

export const UserLiteModel = new Type<UserLiteModel>("UserLiteModel");
export interface UserLiteModel extends Entities.ModelEntity {
  Type: "UserLiteModel";
  userName: string;
  toStringValue: string | null;
  oID: string /*Guid*/ | null;
  sID: string | null;
  customPhotoHash: string | null;
}

export module UserOperation {
  export const Create : Operations.ConstructSymbol_Simple<UserEntity> = registerSymbol("Operation", "UserOperation.Create");
  export const Save : Operations.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.Save");
  export const Reactivate : Operations.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.Reactivate");
  export const Deactivate : Operations.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.Deactivate");
  export const SetPassword : Operations.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.SetPassword");
  export const Delete : Operations.DeleteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.Delete");
}

export const UserState = new EnumType<UserState>("UserState");
export type UserState =
  "New" |
  "Active" |
  "Deactivated";

export module UserTypeCondition {
  export const DeactivatedUsers : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "UserTypeCondition.DeactivatedUsers");
}

