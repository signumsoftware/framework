//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Scripts/Reflection'
import * as Entities from '../../Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../Signum.React/Scripts/Signum.Entities.Basics'
import * as Signum from '../Basics/Signum.Entities.Basics'
import * as Mailing from '../Mailing/Signum.Entities.Mailing'
import * as Scheduler from '../Scheduler/Signum.Entities.Scheduler'

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
  directoryRegistry_Username: string | null;
  directoryRegistry_Password: string | null;
  azure_ApplicationID: string /*Guid*/ | null;
  azure_DirectoryID: string /*Guid*/ | null;
  azure_ClientSecret: string | null;
  loginWithWindowsAuthenticator: boolean;
  loginWithActiveDirectoryRegistry: boolean;
  loginWithAzureAD: boolean;
  allowMatchUsersBySimpleUserName: boolean;
  autoCreateUsers: boolean;
  autoUpdateUsers: boolean;
  roleMapping: Entities.MList<RoleMappingEmbedded>;
  defaultRole: Entities.Lite<RoleEntity> | null;
}

export module ActiveDirectoryMessage {
  export const Id = new MessageKey("ActiveDirectoryMessage", "Id");
  export const DisplayName = new MessageKey("ActiveDirectoryMessage", "DisplayName");
  export const Mail = new MessageKey("ActiveDirectoryMessage", "Mail");
  export const GivenName = new MessageKey("ActiveDirectoryMessage", "GivenName");
  export const Surname = new MessageKey("ActiveDirectoryMessage", "Surname");
  export const JobTitle = new MessageKey("ActiveDirectoryMessage", "JobTitle");
  export const OnPremisesImmutableId = new MessageKey("ActiveDirectoryMessage", "OnPremisesImmutableId");
  export const CompanyName = new MessageKey("ActiveDirectoryMessage", "CompanyName");
  export const AccountEnabled = new MessageKey("ActiveDirectoryMessage", "AccountEnabled");
  export const OnPremisesExtensionAttributes = new MessageKey("ActiveDirectoryMessage", "OnPremisesExtensionAttributes");
  export const OnlyActiveUsers = new MessageKey("ActiveDirectoryMessage", "OnlyActiveUsers");
  export const InGroup = new MessageKey("ActiveDirectoryMessage", "InGroup");
  export const Description = new MessageKey("ActiveDirectoryMessage", "Description");
  export const SecurityEnabled = new MessageKey("ActiveDirectoryMessage", "SecurityEnabled");
  export const Visibility = new MessageKey("ActiveDirectoryMessage", "Visibility");
  export const HasUser = new MessageKey("ActiveDirectoryMessage", "HasUser");
}

export module ActiveDirectoryPermission {
  export const InviteUsersFromAD : PermissionSymbol = registerSymbol("Permission", "ActiveDirectoryPermission.InviteUsersFromAD");
}

export module ActiveDirectoryTask {
  export const DeactivateUsers : Scheduler.SimpleTaskSymbol = registerSymbol("SimpleTask", "ActiveDirectoryTask.DeactivateUsers");
}

export const ADGroupEntity = new Type<ADGroupEntity>("ADGroup");
export interface ADGroupEntity extends Entities.Entity {
  Type: "ADGroup";
  displayName: string;
}

export module ADGroupOperation {
  export const Save : Entities.ExecuteSymbol<ADGroupEntity> = registerSymbol("Operation", "ADGroupOperation.Save");
  export const Delete : Entities.DeleteSymbol<ADGroupEntity> = registerSymbol("Operation", "ADGroupOperation.Delete");
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
  export const TypeRules = new MessageKey("AuthAdminMessage", "TypeRules");
  export const PermissionRules = new MessageKey("AuthAdminMessage", "PermissionRules");
  export const Allow = new MessageKey("AuthAdminMessage", "Allow");
  export const Deny = new MessageKey("AuthAdminMessage", "Deny");
  export const Overriden = new MessageKey("AuthAdminMessage", "Overriden");
  export const Filter = new MessageKey("AuthAdminMessage", "Filter");
  export const PleaseSaveChangesFirst = new MessageKey("AuthAdminMessage", "PleaseSaveChangesFirst");
  export const ResetChanges = new MessageKey("AuthAdminMessage", "ResetChanges");
  export const SwitchTo = new MessageKey("AuthAdminMessage", "SwitchTo");
  export const OnlyActive = new MessageKey("AuthAdminMessage", "OnlyActive");
  export const _0InUI = new MessageKey("AuthAdminMessage", "_0InUI");
  export const _0InDB = new MessageKey("AuthAdminMessage", "_0InDB");
  export const CanNotBeModified = new MessageKey("AuthAdminMessage", "CanNotBeModified");
  export const CanNotBeModifiedBecauseIsInCondition0 = new MessageKey("AuthAdminMessage", "CanNotBeModifiedBecauseIsInCondition0");
  export const CanNotBeModifiedBecauseIsNotInCondition0 = new MessageKey("AuthAdminMessage", "CanNotBeModifiedBecauseIsNotInCondition0");
  export const CanNotBeReadBecauseIsInCondition0 = new MessageKey("AuthAdminMessage", "CanNotBeReadBecauseIsInCondition0");
  export const CanNotBeReadBecauseIsNotInCondition0 = new MessageKey("AuthAdminMessage", "CanNotBeReadBecauseIsNotInCondition0");
  export const _0RulesFor1 = new MessageKey("AuthAdminMessage", "_0RulesFor1");
  export const TheUserStateMustBeDisabled = new MessageKey("AuthAdminMessage", "TheUserStateMustBeDisabled");
  export const _0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships = new MessageKey("AuthAdminMessage", "_0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships");
  export const ConflictMergingTypeConditions = new MessageKey("AuthAdminMessage", "ConflictMergingTypeConditions");
  export const Save = new MessageKey("AuthAdminMessage", "Save");
  export const DefaultAuthorization = new MessageKey("AuthAdminMessage", "DefaultAuthorization");
  export const MaximumOfThe0 = new MessageKey("AuthAdminMessage", "MaximumOfThe0");
  export const MinumumOfThe0 = new MessageKey("AuthAdminMessage", "MinumumOfThe0");
  export const SameAs0 = new MessageKey("AuthAdminMessage", "SameAs0");
  export const Nothing = new MessageKey("AuthAdminMessage", "Nothing");
  export const Everything = new MessageKey("AuthAdminMessage", "Everything");
  export const SelectTypeConditions = new MessageKey("AuthAdminMessage", "SelectTypeConditions");
  export const ThereAre0TypeConditionsDefinedFor1 = new MessageKey("AuthAdminMessage", "ThereAre0TypeConditionsDefinedFor1");
  export const SelectOneToOverrideTheAccessFor0ThatSatisfyThisCondition = new MessageKey("AuthAdminMessage", "SelectOneToOverrideTheAccessFor0ThatSatisfyThisCondition");
  export const SelectMoreThanOneToOverrideAccessFor0ThatSatisfyAllTheConditionsAtTheSameTime = new MessageKey("AuthAdminMessage", "SelectMoreThanOneToOverrideAccessFor0ThatSatisfyAllTheConditionsAtTheSameTime");
  export const RepeatedTypeCondition = new MessageKey("AuthAdminMessage", "RepeatedTypeCondition");
  export const TheFollowingTypeConditionsHaveAlreadyBeenUsed = new MessageKey("AuthAdminMessage", "TheFollowingTypeConditionsHaveAlreadyBeenUsed");
  export const Role0InheritsFromTrivialMergeRole1 = new MessageKey("AuthAdminMessage", "Role0InheritsFromTrivialMergeRole1");
  export const IncludeTrivialMerges = new MessageKey("AuthAdminMessage", "IncludeTrivialMerges");
  export const Role0IsTrivialMerge = new MessageKey("AuthAdminMessage", "Role0IsTrivialMerge");
}

export module AuthEmailMessage {
  export const YouRecentlyRequestedANewPassword = new MessageKey("AuthEmailMessage", "YouRecentlyRequestedANewPassword");
  export const YourUsernameIs = new MessageKey("AuthEmailMessage", "YourUsernameIs");
  export const YouCanResetYourPasswordByFollowingTheLinkBelow = new MessageKey("AuthEmailMessage", "YouCanResetYourPasswordByFollowingTheLinkBelow");
  export const ResetPasswordRequestSubject = new MessageKey("AuthEmailMessage", "ResetPasswordRequestSubject");
  export const YourResetPasswordRequestHasExpired = new MessageKey("AuthEmailMessage", "YourResetPasswordRequestHasExpired");
  export const WeHaveSendYouAnEmailToResetYourPassword = new MessageKey("AuthEmailMessage", "WeHaveSendYouAnEmailToResetYourPassword");
  export const EmailNotFound = new MessageKey("AuthEmailMessage", "EmailNotFound");
  export const YourAccountHasBeenLockedDueToSeveralFailedLogins = new MessageKey("AuthEmailMessage", "YourAccountHasBeenLockedDueToSeveralFailedLogins");
  export const YourAccountHasBeenLocked = new MessageKey("AuthEmailMessage", "YourAccountHasBeenLocked");
}

export module AuthMessage {
  export const NotAuthorizedTo0The1WithId2 = new MessageKey("AuthMessage", "NotAuthorizedTo0The1WithId2");
  export const NotAuthorizedToRetrieve0 = new MessageKey("AuthMessage", "NotAuthorizedToRetrieve0");
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
  refreshAnyTokenPreviousTo: string /*DateTime*/ | null;
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

export module LoginAuthMessage {
  export const ThePasswordMustHaveAtLeast0Characters = new MessageKey("LoginAuthMessage", "ThePasswordMustHaveAtLeast0Characters");
  export const NotUserLogged = new MessageKey("LoginAuthMessage", "NotUserLogged");
  export const Username0IsNotValid = new MessageKey("LoginAuthMessage", "Username0IsNotValid");
  export const User0IsDisabled = new MessageKey("LoginAuthMessage", "User0IsDisabled");
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
}

export const MergeStrategy = new EnumType<MergeStrategy>("MergeStrategy");
export type MergeStrategy =
  "Union" |
  "Intersection";

export const OnPremisesExtensionAttributesModel = new Type<OnPremisesExtensionAttributesModel>("OnPremisesExtensionAttributesModel");
export interface OnPremisesExtensionAttributesModel extends Entities.ModelEntity {
  Type: "OnPremisesExtensionAttributesModel";
  extensionAttribute1: string;
  extensionAttribute2: string;
  extensionAttribute3: string;
  extensionAttribute4: string;
  extensionAttribute5: string;
  extensionAttribute6: string;
  extensionAttribute7: string;
  extensionAttribute8: string;
  extensionAttribute9: string;
  extensionAttribute10: string;
  extensionAttribute11: string;
  extensionAttribute12: string;
  extensionAttribute13: string;
  extensionAttribute14: string;
  extensionAttribute15: string;
}

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
  requestDate: string /*DateTime*/;
  used: boolean;
}

export module ResetPasswordRequestOperation {
  export const Execute : Entities.ExecuteSymbol<ResetPasswordRequestEntity> = registerSymbol("Operation", "ResetPasswordRequestOperation.Execute");
}

export const RoleEntity = new Type<RoleEntity>("Role");
export interface RoleEntity extends Entities.Entity {
  Type: "Role";
  name: string;
  mergeStrategy: MergeStrategy;
  isTrivialMerge: boolean;
  inheritsFrom: Entities.MList<Entities.Lite<RoleEntity>>;
  description: string | null;
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

export const RuleTypeConditionEntity = new Type<RuleTypeConditionEntity>("RuleTypeCondition");
export interface RuleTypeConditionEntity extends Entities.Entity {
  Type: "RuleTypeCondition";
  ruleType: Entities.Lite<RuleTypeEntity>;
  conditions: Entities.MList<Signum.TypeConditionSymbol>;
  allowed: TypeAllowed;
  order: number;
}

export const RuleTypeEntity = new Type<RuleTypeEntity>("RuleType");
export interface RuleTypeEntity extends RuleEntity<Basics.TypeEntity, TypeAllowed> {
  Type: "RuleType";
  conditionRules: Entities.MList<RuleTypeConditionEntity>;
}

export const SessionLogEntity = new Type<SessionLogEntity>("SessionLog");
export interface SessionLogEntity extends Entities.Entity {
  Type: "SessionLog";
  user: Entities.Lite<UserEntity>;
  sessionStart: string /*DateTime*/;
  sessionEnd: string /*DateTime*/ | null;
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
  fallback: TypeAllowed;
  conditionRules: Entities.MList<TypeConditionRuleModel>;
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

export const TypeConditionRuleModel = new Type<TypeConditionRuleModel>("TypeConditionRuleModel");
export interface TypeConditionRuleModel extends Entities.ModelEntity {
  Type: "TypeConditionRuleModel";
  typeConditions: Entities.MList<Signum.TypeConditionSymbol>;
  allowed: TypeAllowed;
}

export const TypeRulePack = new Type<TypeRulePack>("TypeRulePack");
export interface TypeRulePack extends BaseRulePack<TypeAllowedRule> {
  Type: "TypeRulePack";
}

export module UserADMessage {
  export const Find0InActiveDirectory = new MessageKey("UserADMessage", "Find0InActiveDirectory");
  export const FindInActiveDirectory = new MessageKey("UserADMessage", "FindInActiveDirectory");
  export const NoUserContaining0FoundInActiveDirectory = new MessageKey("UserADMessage", "NoUserContaining0FoundInActiveDirectory");
  export const SelectActiveDirectoryUser = new MessageKey("UserADMessage", "SelectActiveDirectoryUser");
  export const PleaseSelectTheUserFromActiveDirectoryThatYouWantToImport = new MessageKey("UserADMessage", "PleaseSelectTheUserFromActiveDirectoryThatYouWantToImport");
  export const NameOrEmail = new MessageKey("UserADMessage", "NameOrEmail");
}

export const UserADMixin = new Type<UserADMixin>("UserADMixin");
export interface UserADMixin extends Entities.MixinEntity {
  Type: "UserADMixin";
  oID: string /*Guid*/ | null;
  sID: string | null;
}

export module UserADQuery {
  export const ActiveDirectoryUsers = new QueryKey("UserADQuery", "ActiveDirectoryUsers");
  export const ActiveDirectoryGroups = new QueryKey("UserADQuery", "ActiveDirectoryGroups");
}

export const UserEntity = new Type<UserEntity>("User");
export interface UserEntity extends Entities.Entity, Mailing.IEmailOwnerEntity, Basics.IUserEntity {
  Type: "User";
  userName: string;
  passwordHash: string /*Byte[]*/ | null;
  role: Entities.Lite<RoleEntity>;
  email: string | null;
  cultureInfo: Signum.CultureInfoEntity | null;
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
}

export module UserOIDMessage {
  export const TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet = new MessageKey("UserOIDMessage", "TheUser0IsConnectedToActiveDirectoryAndCanNotHaveALocalPasswordSet");
}

export module UserOperation {
  export const Create : Entities.ConstructSymbol_Simple<UserEntity> = registerSymbol("Operation", "UserOperation.Create");
  export const Save : Entities.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.Save");
  export const Reactivate : Entities.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.Reactivate");
  export const Deactivate : Entities.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.Deactivate");
  export const SetPassword : Entities.ExecuteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.SetPassword");
  export const Delete : Entities.DeleteSymbol<UserEntity> = registerSymbol("Operation", "UserOperation.Delete");
}

export const UserState = new EnumType<UserState>("UserState");
export type UserState =
  "New" |
  "Active" |
  "Deactivated";

export const UserTicketEntity = new Type<UserTicketEntity>("UserTicket");
export interface UserTicketEntity extends Entities.Entity {
  Type: "UserTicket";
  user: Entities.Lite<UserEntity>;
  ticket: string;
  connectionDate: string /*DateTime*/;
  device: string;
}

export module UserTypeCondition {
  export const DeactivatedUsers : Signum.TypeConditionSymbol = registerSymbol("TypeCondition", "UserTypeCondition.DeactivatedUsers");
}


