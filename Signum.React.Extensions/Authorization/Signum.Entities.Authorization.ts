//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Signum from '../Basics/Signum.Entities.Basics'
import * as Mailing from '../Mailing/Signum.Entities.Mailing'

export interface UserEntity {
    newPassword: string;
}

export module ActiveDirectoryAuthorizerMessage {
  export const ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication = new MessageKey("ActiveDirectoryAuthorizerMessage", "ActiveDirectoryUser0IsNotAssociatedWithAUserInThisApplication");
}

export const ActiveDirectoryConfigurationEmbedded = new Type<ActiveDirectoryConfigurationEmbedded>("ActiveDirectoryConfigurationEmbedded");
export interface ActiveDirectoryConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "ActiveDirectoryConfigurationEmbedded";
  domainName: string | null;
  domainServer: string | null;
  azure_ApplicationID: string | null;
  azure_DirectoryID: string | null;
  loginWithWindowsAuthenticator: boolean;
  loginWithActiveDirectoryRegistry: boolean;
  loginWithAzureAD: boolean;
  allowSimpleUserNames: boolean;
  autoCreateUsers: boolean;
  roleMapping: Entities.MList<RoleMappingEmbedded>;
  defaultRole: Entities.Lite<RoleEntity> | null;
}

export interface AllowedRule<R, A> extends Entities.ModelEntity {
  allowedBase: A;
  allowed: A;
  resource: R;
}

export interface AllowedRuleCoerced<R, A> extends AllowedRule<R, A> {
  coercedValues: A[];
}

export module AuthAdminMessage {
  export const _0of1 = new MessageKey("AuthAdminMessage", "_0of1");
  export const Nothing = new MessageKey("AuthAdminMessage", "Nothing");
  export const Everything = new MessageKey("AuthAdminMessage", "Everything");
  export const TypeRules = new MessageKey("AuthAdminMessage", "TypeRules");
  export const PermissionRules = new MessageKey("AuthAdminMessage", "PermissionRules");
  export const Allow = new MessageKey("AuthAdminMessage", "Allow");
  export const Deny = new MessageKey("AuthAdminMessage", "Deny");
  export const Overriden = new MessageKey("AuthAdminMessage", "Overriden");
  export const NoRoles = new MessageKey("AuthAdminMessage", "NoRoles");
  export const Filter = new MessageKey("AuthAdminMessage", "Filter");
  export const PleaseSaveChangesFirst = new MessageKey("AuthAdminMessage", "PleaseSaveChangesFirst");
  export const ResetChanges = new MessageKey("AuthAdminMessage", "ResetChanges");
  export const SwitchTo = new MessageKey("AuthAdminMessage", "SwitchTo");
  export const _0InUI = new MessageKey("AuthAdminMessage", "_0InUI");
  export const _0InDB = new MessageKey("AuthAdminMessage", "_0InDB");
  export const CanNotBeModified = new MessageKey("AuthAdminMessage", "CanNotBeModified");
  export const CanNotBeModifiedBecauseIsA0 = new MessageKey("AuthAdminMessage", "CanNotBeModifiedBecauseIsA0");
  export const CanNotBeModifiedBecauseIsNotA0 = new MessageKey("AuthAdminMessage", "CanNotBeModifiedBecauseIsNotA0");
}

export module AuthEmailMessage {
  export const YouRecentlyRequestedANewPassword = new MessageKey("AuthEmailMessage", "YouRecentlyRequestedANewPassword");
  export const YourUsernameIs = new MessageKey("AuthEmailMessage", "YourUsernameIs");
  export const YouCanResetYourPasswordByFollowingTheLinkBelow = new MessageKey("AuthEmailMessage", "YouCanResetYourPasswordByFollowingTheLinkBelow");
  export const ResetPasswordRequestSubject = new MessageKey("AuthEmailMessage", "ResetPasswordRequestSubject");
  export const YourResetPasswordRequestHasExpired = new MessageKey("AuthEmailMessage", "YourResetPasswordRequestHasExpired");
  export const WeHaveSendYouAnEmailToResetYourPassword = new MessageKey("AuthEmailMessage", "WeHaveSendYouAnEmailToResetYourPassword");
  export const EmailNotFound = new MessageKey("AuthEmailMessage", "EmailNotFound");
}

export module AuthMessage {
  export const _0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships = new MessageKey("AuthMessage", "_0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships");
  export const _0RulesFor1 = new MessageKey("AuthMessage", "_0RulesFor1");
  export const AuthAdmin_AddCondition = new MessageKey("AuthMessage", "AuthAdmin_AddCondition");
  export const AuthAdmin_ChooseACondition = new MessageKey("AuthMessage", "AuthAdmin_ChooseACondition");
  export const AuthAdmin_RemoveCondition = new MessageKey("AuthMessage", "AuthAdmin_RemoveCondition");
  export const AuthorizationCacheSuccessfullyUpdated = new MessageKey("AuthMessage", "AuthorizationCacheSuccessfullyUpdated");
  export const ChangePassword = new MessageKey("AuthMessage", "ChangePassword");
  export const ChangePasswordAspx_ActualPassword = new MessageKey("AuthMessage", "ChangePasswordAspx_ActualPassword");
  export const ChangePasswordAspx_ChangePassword = new MessageKey("AuthMessage", "ChangePasswordAspx_ChangePassword");
  export const ChangePasswordAspx_ConfirmNewPassword = new MessageKey("AuthMessage", "ChangePasswordAspx_ConfirmNewPassword");
  export const ChangePasswordAspx_NewPassword = new MessageKey("AuthMessage", "ChangePasswordAspx_NewPassword");
  export const ChangePasswordAspx_EnterActualPasswordAndNewOne = new MessageKey("AuthMessage", "ChangePasswordAspx_EnterActualPasswordAndNewOne");
  export const ConfirmNewPassword = new MessageKey("AuthMessage", "ConfirmNewPassword");
  export const EmailMustHaveAValue = new MessageKey("AuthMessage", "EmailMustHaveAValue");
  export const EmailSent = new MessageKey("AuthMessage", "EmailSent");
  export const Email = new MessageKey("AuthMessage", "Email");
  export const EnterTheNewPassword = new MessageKey("AuthMessage", "EnterTheNewPassword");
  export const EntityGroupsAscx_EntityGroup = new MessageKey("AuthMessage", "EntityGroupsAscx_EntityGroup");
  export const EntityGroupsAscx_Overriden = new MessageKey("AuthMessage", "EntityGroupsAscx_Overriden");
  export const ExpectedUserLogged = new MessageKey("AuthMessage", "ExpectedUserLogged");
  export const ExpiredPassword = new MessageKey("AuthMessage", "ExpiredPassword");
  export const ExpiredPasswordMessage = new MessageKey("AuthMessage", "ExpiredPasswordMessage");
  export const ForgotYourPasswordEnterYourPasswordBelow = new MessageKey("AuthMessage", "ForgotYourPasswordEnterYourPasswordBelow");
  export const WeWillSendYouAnEmailWithALinkToResetYourPassword = new MessageKey("AuthMessage", "WeWillSendYouAnEmailWithALinkToResetYourPassword");
  export const IHaveForgottenMyPassword = new MessageKey("AuthMessage", "IHaveForgottenMyPassword");
  export const IncorrectPassword = new MessageKey("AuthMessage", "IncorrectPassword");
  export const EnterYourUserNameAndPassword = new MessageKey("AuthMessage", "EnterYourUserNameAndPassword");
  export const InvalidUsernameOrPassword = new MessageKey("AuthMessage", "InvalidUsernameOrPassword");
  export const InvalidUsername = new MessageKey("AuthMessage", "InvalidUsername");
  export const InvalidPassword = new MessageKey("AuthMessage", "InvalidPassword");
  export const Login_New = new MessageKey("AuthMessage", "Login_New");
  export const Login_Password = new MessageKey("AuthMessage", "Login_Password");
  export const Login_Repeat = new MessageKey("AuthMessage", "Login_Repeat");
  export const Login_UserName = new MessageKey("AuthMessage", "Login_UserName");
  export const Login_UserName_Watermark = new MessageKey("AuthMessage", "Login_UserName_Watermark");
  export const Login = new MessageKey("AuthMessage", "Login");
  export const Logout = new MessageKey("AuthMessage", "Logout");
  export const SwitchUser = new MessageKey("AuthMessage", "SwitchUser");
  export const NewPassword = new MessageKey("AuthMessage", "NewPassword");
  export const NotAllowedToSaveThis0WhileOffline = new MessageKey("AuthMessage", "NotAllowedToSaveThis0WhileOffline");
  export const NotAuthorizedTo0The1WithId2 = new MessageKey("AuthMessage", "NotAuthorizedTo0The1WithId2");
  export const NotAuthorizedToRetrieve0 = new MessageKey("AuthMessage", "NotAuthorizedToRetrieve0");
  export const NotAuthorizedToSave0 = new MessageKey("AuthMessage", "NotAuthorizedToSave0");
  export const NotAuthorizedToChangeProperty0on1 = new MessageKey("AuthMessage", "NotAuthorizedToChangeProperty0on1");
  export const NotUserLogged = new MessageKey("AuthMessage", "NotUserLogged");
  export const Password = new MessageKey("AuthMessage", "Password");
  export const PasswordChanged = new MessageKey("AuthMessage", "PasswordChanged");
  export const PasswordDoesNotMatchCurrent = new MessageKey("AuthMessage", "PasswordDoesNotMatchCurrent");
  export const PasswordHasBeenChangedSuccessfully = new MessageKey("AuthMessage", "PasswordHasBeenChangedSuccessfully");
  export const PasswordMustHaveAValue = new MessageKey("AuthMessage", "PasswordMustHaveAValue");
  export const AnErrorOccurredRequestNotProcessed = new MessageKey("AuthMessage", "AnErrorOccurredRequestNotProcessed");
  export const WeHaveSentYouAnEmailToResetYourPassword = new MessageKey("AuthMessage", "WeHaveSentYouAnEmailToResetYourPassword");
  export const EnterYourUserEmail = new MessageKey("AuthMessage", "EnterYourUserEmail");
  export const RequestAccepted = new MessageKey("AuthMessage", "RequestAccepted");
  export const YourPasswordIsNearExpiration = new MessageKey("AuthMessage", "YourPasswordIsNearExpiration");
  export const PasswordsAreDifferent = new MessageKey("AuthMessage", "PasswordsAreDifferent");
  export const PasswordsDoNotMatch = new MessageKey("AuthMessage", "PasswordsDoNotMatch");
  export const Please0IntoYourAccount = new MessageKey("AuthMessage", "Please0IntoYourAccount");
  export const PleaseEnterYourChosenNewPassword = new MessageKey("AuthMessage", "PleaseEnterYourChosenNewPassword");
  export const Remember = new MessageKey("AuthMessage", "Remember");
  export const RememberMe = new MessageKey("AuthMessage", "RememberMe");
  export const ResetPassword = new MessageKey("AuthMessage", "ResetPassword");
  export const ResetPasswordCode = new MessageKey("AuthMessage", "ResetPasswordCode");
  export const ResetPasswordCodeHasBeenSent = new MessageKey("AuthMessage", "ResetPasswordCodeHasBeenSent");
  export const ResetPasswordSuccess = new MessageKey("AuthMessage", "ResetPasswordSuccess");
  export const Save = new MessageKey("AuthMessage", "Save");
  export const TheConfirmationCodeThatYouHaveJustSentIsInvalid = new MessageKey("AuthMessage", "TheConfirmationCodeThatYouHaveJustSentIsInvalid");
  export const ThePasswordMustHaveAtLeast5Characters = new MessageKey("AuthMessage", "ThePasswordMustHaveAtLeast5Characters");
  export const ThereHasBeenAnErrorWithYourRequestToResetYourPasswordPleaseEnterYourLogin = new MessageKey("AuthMessage", "ThereHasBeenAnErrorWithYourRequestToResetYourPasswordPleaseEnterYourLogin");
  export const ThereSNotARegisteredUserWithThatEmailAddress = new MessageKey("AuthMessage", "ThereSNotARegisteredUserWithThatEmailAddress");
  export const TheSpecifiedPasswordsDontMatch = new MessageKey("AuthMessage", "TheSpecifiedPasswordsDontMatch");
  export const TheUserStateMustBeDisabled = new MessageKey("AuthMessage", "TheUserStateMustBeDisabled");
  export const Username = new MessageKey("AuthMessage", "Username");
  export const Username0IsNotValid = new MessageKey("AuthMessage", "Username0IsNotValid");
  export const UserNameMustHaveAValue = new MessageKey("AuthMessage", "UserNameMustHaveAValue");
  export const View = new MessageKey("AuthMessage", "View");
  export const WeReceivedARequestToCreateAnAccountYouCanCreateItFollowingTheLinkBelow = new MessageKey("AuthMessage", "WeReceivedARequestToCreateAnAccountYouCanCreateItFollowingTheLinkBelow");
  export const YouMustRepeatTheNewPassword = new MessageKey("AuthMessage", "YouMustRepeatTheNewPassword");
  export const User0IsDisabled = new MessageKey("AuthMessage", "User0IsDisabled");
  export const SendEmail = new MessageKey("AuthMessage", "SendEmail");
  export const Welcome0 = new MessageKey("AuthMessage", "Welcome0");
  export const LoginWithAnotherUser = new MessageKey("AuthMessage", "LoginWithAnotherUser");
  export const TheUserIsNotLongerInTheDatabase = new MessageKey("AuthMessage", "TheUserIsNotLongerInTheDatabase");
  export const IForgotMyPassword = new MessageKey("AuthMessage", "IForgotMyPassword");
  export const GiveUsYourUserEmailToResetYourPassword = new MessageKey("AuthMessage", "GiveUsYourUserEmailToResetYourPassword");
  export const LoginWithWindowsUser = new MessageKey("AuthMessage", "LoginWithWindowsUser");
  export const NoWindowsUserFound = new MessageKey("AuthMessage", "NoWindowsUserFound");
  export const LooksLikeYourWindowsUserIsNotAllowedToUseThisApplication = new MessageKey("AuthMessage", "LooksLikeYourWindowsUserIsNotAllowedToUseThisApplication");
}

export const AuthThumbnail = new EnumType<AuthThumbnail>("AuthThumbnail");
export type AuthThumbnail =
  "All" |
  "Mix" |
  "None";

export const AuthTokenConfigurationEmbedded = new Type<AuthTokenConfigurationEmbedded>("AuthTokenConfigurationEmbedded");
export interface AuthTokenConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "AuthTokenConfigurationEmbedded";
  refreshTokenEvery: number;
  refreshAnyTokenPreviousTo: string | null;
}

export interface BaseRulePack<T> extends Entities.ModelEntity {
  role: Entities.Lite<RoleEntity>;
  strategy: string;
  rules: Entities.MList<T>;
}

export module BasicPermission {
  export const AdminRules : PermissionSymbol = registerSymbol("Permission", "BasicPermission.AdminRules");
  export const AutomaticUpgradeOfProperties : PermissionSymbol = registerSymbol("Permission", "BasicPermission.AutomaticUpgradeOfProperties");
  export const AutomaticUpgradeOfQueries : PermissionSymbol = registerSymbol("Permission", "BasicPermission.AutomaticUpgradeOfQueries");
  export const AutomaticUpgradeOfOperations : PermissionSymbol = registerSymbol("Permission", "BasicPermission.AutomaticUpgradeOfOperations");
}

export const MergeStrategy = new EnumType<MergeStrategy>("MergeStrategy");
export type MergeStrategy =
  "Union" |
  "Intersection";

export const OperationAllowed = new EnumType<OperationAllowed>("OperationAllowed");
export type OperationAllowed =
  "None" |
  "DBOnly" |
  "Allow";

export const OperationAllowedRule = new Type<OperationAllowedRule>("OperationAllowedRule");
export interface OperationAllowedRule extends AllowedRuleCoerced<OperationTypeEmbedded, OperationAllowed> {
  Type: "OperationAllowedRule";
}

export const OperationRulePack = new Type<OperationRulePack>("OperationRulePack");
export interface OperationRulePack extends BaseRulePack<OperationAllowedRule> {
  Type: "OperationRulePack";
  type: Basics.TypeEntity;
}

export const OperationTypeEmbedded = new Type<OperationTypeEmbedded>("OperationTypeEmbedded");
export interface OperationTypeEmbedded extends Entities.EmbeddedEntity {
  Type: "OperationTypeEmbedded";
  operation: Entities.OperationSymbol;
  type: Basics.TypeEntity;
}

export const PasswordExpiresIntervalEntity = new Type<PasswordExpiresIntervalEntity>("PasswordExpiresInterval");
export interface PasswordExpiresIntervalEntity extends Entities.Entity {
  Type: "PasswordExpiresInterval";
  days: number;
  daysWarning: number;
  enabled: boolean;
}

export module PasswordExpiresIntervalOperation {
  export const Save : Entities.ExecuteSymbol<PasswordExpiresIntervalEntity> = registerSymbol("Operation", "PasswordExpiresIntervalOperation.Save");
}

export const PermissionAllowedRule = new Type<PermissionAllowedRule>("PermissionAllowedRule");
export interface PermissionAllowedRule extends AllowedRule<PermissionSymbol, boolean> {
  Type: "PermissionAllowedRule";
}

export const PermissionRulePack = new Type<PermissionRulePack>("PermissionRulePack");
export interface PermissionRulePack extends BaseRulePack<PermissionAllowedRule> {
  Type: "PermissionRulePack";
}

export const PermissionSymbol = new Type<PermissionSymbol>("Permission");
export interface PermissionSymbol extends Entities.Symbol {
  Type: "Permission";
}

export const PropertyAllowed = new EnumType<PropertyAllowed>("PropertyAllowed");
export type PropertyAllowed =
  "None" |
  "Read" |
  "Write";

export const PropertyAllowedRule = new Type<PropertyAllowedRule>("PropertyAllowedRule");
export interface PropertyAllowedRule extends AllowedRuleCoerced<Basics.PropertyRouteEntity, PropertyAllowed> {
  Type: "PropertyAllowedRule";
}

export const PropertyRulePack = new Type<PropertyRulePack>("PropertyRulePack");
export interface PropertyRulePack extends BaseRulePack<PropertyAllowedRule> {
  Type: "PropertyRulePack";
  type: Basics.TypeEntity;
}

export const QueryAllowed = new EnumType<QueryAllowed>("QueryAllowed");
export type QueryAllowed =
  "None" |
  "EmbeddedOnly" |
  "Allow";

export const QueryAllowedRule = new Type<QueryAllowedRule>("QueryAllowedRule");
export interface QueryAllowedRule extends AllowedRuleCoerced<Basics.QueryEntity, QueryAllowed> {
  Type: "QueryAllowedRule";
}

export const QueryRulePack = new Type<QueryRulePack>("QueryRulePack");
export interface QueryRulePack extends BaseRulePack<QueryAllowedRule> {
  Type: "QueryRulePack";
  type: Basics.TypeEntity;
}

export const ResetPasswordRequestEntity = new Type<ResetPasswordRequestEntity>("ResetPasswordRequest");
export interface ResetPasswordRequestEntity extends Entities.Entity {
  Type: "ResetPasswordRequest";
  code: string;
  user: UserEntity;
  requestDate: string;
  lapsed: boolean;
}

export module ResetPasswordRequestOperation {
  export const Execute : Entities.ExecuteSymbol<ResetPasswordRequestEntity> = registerSymbol("Operation", "ResetPasswordRequestOperation.Execute");
}

export const RoleEntity = new Type<RoleEntity>("Role");
export interface RoleEntity extends Entities.Entity {
  Type: "Role";
  name: string;
  mergeStrategy: MergeStrategy;
  roles: Entities.MList<Entities.Lite<RoleEntity>>;
}

export const RoleMappingEmbedded = new Type<RoleMappingEmbedded>("RoleMappingEmbedded");
export interface RoleMappingEmbedded extends Entities.EmbeddedEntity {
  Type: "RoleMappingEmbedded";
  aDNameOrGuid: string;
  role: Entities.Lite<RoleEntity>;
}

export module RoleOperation {
  export const Save : Entities.ExecuteSymbol<RoleEntity> = registerSymbol("Operation", "RoleOperation.Save");
  export const Delete : Entities.DeleteSymbol<RoleEntity> = registerSymbol("Operation", "RoleOperation.Delete");
}

export module RoleQuery {
  export const RolesReferedBy = new QueryKey("RoleQuery", "RolesReferedBy");
}

export interface RuleEntity<R, A> extends Entities.Entity {
  role: Entities.Lite<RoleEntity>;
  resource: R;
  allowed: A;
}

export const RuleOperationEntity = new Type<RuleOperationEntity>("RuleOperation");
export interface RuleOperationEntity extends RuleEntity<OperationTypeEmbedded, OperationAllowed> {
  Type: "RuleOperation";
}

export const RulePermissionEntity = new Type<RulePermissionEntity>("RulePermission");
export interface RulePermissionEntity extends RuleEntity<PermissionSymbol, boolean> {
  Type: "RulePermission";
}

export const RulePropertyEntity = new Type<RulePropertyEntity>("RuleProperty");
export interface RulePropertyEntity extends RuleEntity<Basics.PropertyRouteEntity, PropertyAllowed> {
  Type: "RuleProperty";
}

export const RuleQueryEntity = new Type<RuleQueryEntity>("RuleQuery");
export interface RuleQueryEntity extends RuleEntity<Basics.QueryEntity, QueryAllowed> {
  Type: "RuleQuery";
}

export const RuleTypeConditionEmbedded = new Type<RuleTypeConditionEmbedded>("RuleTypeConditionEmbedded");
export interface RuleTypeConditionEmbedded extends Entities.EmbeddedEntity {
  Type: "RuleTypeConditionEmbedded";
  condition: Signum.TypeConditionSymbol;
  allowed: TypeAllowed;
}

export const RuleTypeEntity = new Type<RuleTypeEntity>("RuleType");
export interface RuleTypeEntity extends RuleEntity<Basics.TypeEntity, TypeAllowed> {
  Type: "RuleType";
  conditions: Entities.MList<RuleTypeConditionEmbedded>;
}

export const SessionLogEntity = new Type<SessionLogEntity>("SessionLog");
export interface SessionLogEntity extends Entities.Entity {
  Type: "SessionLog";
  user: Entities.Lite<UserEntity>;
  sessionStart: string;
  sessionEnd: string | null;
  sessionTimeOut: boolean;
  userHostAddress: string | null;
  userAgent: string | null;
}

export module SessionLogPermission {
  export const TrackSession : PermissionSymbol = registerSymbol("Permission", "SessionLogPermission.TrackSession");
}

export const TypeAllowed = new EnumType<TypeAllowed>("TypeAllowed");
export type TypeAllowed =
  "None" |
  "DBReadUINone" |
  "Read" |
  "DBWriteUINone" |
  "DBWriteUIRead" |
  "Write";

export const TypeAllowedAndConditions = new Type<TypeAllowedAndConditions>("TypeAllowedAndConditions");
export interface TypeAllowedAndConditions extends Entities.ModelEntity {
  Type: "TypeAllowedAndConditions";
  fallback: TypeAllowed | null;
  conditions: Entities.MList<TypeConditionRuleEmbedded>;
}

export const TypeAllowedBasic = new EnumType<TypeAllowedBasic>("TypeAllowedBasic");
export type TypeAllowedBasic =
  "None" |
  "Read" |
  "Write";

export const TypeAllowedRule = new Type<TypeAllowedRule>("TypeAllowedRule");
export interface TypeAllowedRule extends AllowedRule<Basics.TypeEntity, TypeAllowedAndConditions> {
  Type: "TypeAllowedRule";
  properties: AuthThumbnail | null;
  operations: AuthThumbnail | null;
  queries: AuthThumbnail | null;
  availableConditions: Array<Signum.TypeConditionSymbol>;
}

export const TypeConditionRuleEmbedded = new Type<TypeConditionRuleEmbedded>("TypeConditionRuleEmbedded");
export interface TypeConditionRuleEmbedded extends Entities.EmbeddedEntity {
  Type: "TypeConditionRuleEmbedded";
  typeCondition: Signum.TypeConditionSymbol;
  allowed: TypeAllowed;
}

export const TypeRulePack = new Type<TypeRulePack>("TypeRulePack");
export interface TypeRulePack extends BaseRulePack<TypeAllowedRule> {
  Type: "TypeRulePack";
}

export const UserEntity = new Type<UserEntity>("User");
export interface UserEntity extends Entities.Entity, Mailing.IEmailOwnerEntity, Basics.IUserEntity {
  Type: "User";
  userName: string;
  passwordHash: string;
  role: Entities.Lite<RoleEntity>;
  email: string | null;
  cultureInfo: Signum.CultureInfoEntity | null;
  disabledOn: string | null;
  state: UserState;
}

export const UserOIDMixin = new Type<UserOIDMixin>("UserOIDMixin");
export interface UserOIDMixin extends Entities.MixinEntity {
  Type: "UserOIDMixin";
  oID: string | null;
}

export module UserOperation {
  export const Create : Entities.ConstructSymbol_Simple<UserEntity> = registerSymbol("Operation", "UserOperation.Create");
  export const Save : Entities.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.Save");
  export const Enable : Entities.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.Enable");
  export const Disable : Entities.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.Disable");
  export const SetPassword : Entities.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.SetPassword");
}

export const UserState = new EnumType<UserState>("UserState");
export type UserState =
  "New" |
  "Saved" |
  "Disabled";

export const UserTicketEntity = new Type<UserTicketEntity>("UserTicket");
export interface UserTicketEntity extends Entities.Entity {
  Type: "UserTicket";
  user: Entities.Lite<UserEntity>;
  ticket: string;
  connectionDate: string;
  device: string;
}


