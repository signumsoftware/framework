//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection' 

import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities' 

import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics' 

import * as ExBasics from '../Basics/Signum.Entities.Basics' 

import * as Mailing from '../Mailing/Signum.Entities.Mailing' 



export interface AllowedRule<R, A> extends Entities.ModelEntity {
    allowedBase?: A;
    allowed?: A;
    overriden?: boolean;
    resource?: R;
}

export interface AllowedRuleCoerced<R, A> extends AllowedRule<R, A> {
    coercedValues?: A[];
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
}

export module AuthEmailMessage {
    export const YouRecentlyRequestedANewPassword = new MessageKey("AuthEmailMessage", "YouRecentlyRequestedANewPassword");
    export const YourUsernameIs = new MessageKey("AuthEmailMessage", "YourUsernameIs");
    export const YouCanResetYourPasswordByFollowingTheLinkBelow = new MessageKey("AuthEmailMessage", "YouCanResetYourPasswordByFollowingTheLinkBelow");
    export const ResetPasswordRequestSubject = new MessageKey("AuthEmailMessage", "ResetPasswordRequestSubject");
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
    export const ChangePasswordAspx_WriteActualPasswordAndNewOne = new MessageKey("AuthMessage", "ChangePasswordAspx_WriteActualPasswordAndNewOne");
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
    export const IntroduceYourUserNameAndPassword = new MessageKey("AuthMessage", "IntroduceYourUserNameAndPassword");
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
    export const ThePasswordMustHaveBetween7And15CharactersEachOfThemBeingANumber09OrALetter = new MessageKey("AuthMessage", "ThePasswordMustHaveBetween7And15CharactersEachOfThemBeingANumber09OrALetter");
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
}

export enum AuthThumbnail {
    All = "All" as any,
    Mix = "Mix" as any,
    None = "None" as any,
}
export const AuthThumbnail_Type = new EnumType<AuthThumbnail>("AuthThumbnail", AuthThumbnail);

export interface BaseRulePack<T> extends Entities.ModelEntity {
    role?: Entities.Lite<RoleEntity>;
    strategy?: string;
    type?: Basics.TypeEntity;
    rules?: Entities.MList<T>;
}

export module BasicPermission {
    export const AdminRules : PermissionSymbol = registerSymbol({ Type: "Permission", key: "BasicPermission.AdminRules" });
    export const AutomaticUpgradeOfProperties : PermissionSymbol = registerSymbol({ Type: "Permission", key: "BasicPermission.AutomaticUpgradeOfProperties" });
    export const AutomaticUpgradeOfQueries : PermissionSymbol = registerSymbol({ Type: "Permission", key: "BasicPermission.AutomaticUpgradeOfQueries" });
    export const AutomaticUpgradeOfOperations : PermissionSymbol = registerSymbol({ Type: "Permission", key: "BasicPermission.AutomaticUpgradeOfOperations" });
}

export const LastAuthRulesImportEntity_Type = new Type<LastAuthRulesImportEntity>("LastAuthRulesImport");
export interface LastAuthRulesImportEntity extends Entities.Entity {
    date?: string;
}

export enum MergeStrategy {
    Union = "Union" as any,
    Intersection = "Intersection" as any,
}
export const MergeStrategy_Type = new EnumType<MergeStrategy>("MergeStrategy", MergeStrategy);

export enum OperationAllowed {
    None = "None" as any,
    DBOnly = "DBOnly" as any,
    Allow = "Allow" as any,
}
export const OperationAllowed_Type = new EnumType<OperationAllowed>("OperationAllowed", OperationAllowed);

export const OperationAllowedRule_Type = new Type<OperationAllowedRule>("OperationAllowedRule");
export interface OperationAllowedRule extends AllowedRuleCoerced<Entities.OperationSymbol, OperationAllowed> {
}

export const OperationRulePack_Type = new Type<OperationRulePack>("OperationRulePack");
export interface OperationRulePack extends BaseRulePack<OperationAllowedRule> {
}

export const PasswordExpiresIntervalEntity_Type = new Type<PasswordExpiresIntervalEntity>("PasswordExpiresInterval");
export interface PasswordExpiresIntervalEntity extends Entities.Entity {
    days?: number;
    daysWarning?: number;
    enabled?: boolean;
}

export module PasswordExpiresIntervalOperation {
    export const Save : Entities.ExecuteSymbol<PasswordExpiresIntervalEntity> = registerSymbol({ Type: "Operation", key: "PasswordExpiresIntervalOperation.Save" });
}

export const PermissionAllowedRule_Type = new Type<PermissionAllowedRule>("PermissionAllowedRule");
export interface PermissionAllowedRule extends AllowedRule<PermissionSymbol, boolean> {
}

export const PermissionRulePack_Type = new Type<PermissionRulePack>("PermissionRulePack");
export interface PermissionRulePack extends BaseRulePack<PermissionAllowedRule> {
}

export const PermissionSymbol_Type = new Type<PermissionSymbol>("Permission");
export interface PermissionSymbol extends Entities.Symbol {
}

export enum PropertyAllowed {
    None = "None" as any,
    Read = "Read" as any,
    Modify = "Modify" as any,
}
export const PropertyAllowed_Type = new EnumType<PropertyAllowed>("PropertyAllowed", PropertyAllowed);

export const PropertyAllowedRule_Type = new Type<PropertyAllowedRule>("PropertyAllowedRule");
export interface PropertyAllowedRule extends AllowedRuleCoerced<Basics.PropertyRouteEntity, PropertyAllowed> {
}

export const PropertyRulePack_Type = new Type<PropertyRulePack>("PropertyRulePack");
export interface PropertyRulePack extends BaseRulePack<PropertyAllowedRule> {
}

export const QueryAllowedRule_Type = new Type<QueryAllowedRule>("QueryAllowedRule");
export interface QueryAllowedRule extends AllowedRuleCoerced<Basics.QueryEntity, boolean> {
}

export const QueryRulePack_Type = new Type<QueryRulePack>("QueryRulePack");
export interface QueryRulePack extends BaseRulePack<QueryAllowedRule> {
}

export const ResetPasswordRequestEntity_Type = new Type<ResetPasswordRequestEntity>("ResetPasswordRequest");
export interface ResetPasswordRequestEntity extends Entities.Entity {
    code?: string;
    user?: UserEntity;
    requestDate?: string;
    lapsed?: boolean;
}

export const RoleEntity_Type = new Type<RoleEntity>("Role");
export interface RoleEntity extends Entities.Entity {
    name?: string;
    mergeStrategy?: MergeStrategy;
    roles?: Entities.MList<Entities.Lite<RoleEntity>>;
}

export module RoleOperation {
    export const Save : Entities.ExecuteSymbol<RoleEntity> = registerSymbol({ Type: "Operation", key: "RoleOperation.Save" });
    export const Delete : Entities.DeleteSymbol<RoleEntity> = registerSymbol({ Type: "Operation", key: "RoleOperation.Delete" });
}

export module RoleQuery {
    export const RolesReferedBy = new MessageKey("RoleQuery", "RolesReferedBy");
}

export interface RuleEntity<R, A> extends Entities.Entity {
    role?: Entities.Lite<RoleEntity>;
    resource?: R;
    allowed?: A;
}

export const RuleOperationEntity_Type = new Type<RuleOperationEntity>("RuleOperation");
export interface RuleOperationEntity extends RuleEntity<Entities.OperationSymbol, OperationAllowed> {
}

export const RulePermissionEntity_Type = new Type<RulePermissionEntity>("RulePermission");
export interface RulePermissionEntity extends RuleEntity<PermissionSymbol, boolean> {
}

export const RulePropertyEntity_Type = new Type<RulePropertyEntity>("RuleProperty");
export interface RulePropertyEntity extends RuleEntity<Basics.PropertyRouteEntity, PropertyAllowed> {
}

export const RuleQueryEntity_Type = new Type<RuleQueryEntity>("RuleQuery");
export interface RuleQueryEntity extends RuleEntity<Basics.QueryEntity, boolean> {
}

export const RuleTypeConditionEntity_Type = new Type<RuleTypeConditionEntity>("RuleTypeConditionEntity");
export interface RuleTypeConditionEntity extends Entities.EmbeddedEntity {
    condition?: ExBasics.TypeConditionSymbol;
    allowed?: TypeAllowed;
}

export const RuleTypeEntity_Type = new Type<RuleTypeEntity>("RuleType");
export interface RuleTypeEntity extends RuleEntity<Basics.TypeEntity, TypeAllowed> {
    conditions?: Entities.MList<RuleTypeConditionEntity>;
}

export const SessionLogEntity_Type = new Type<SessionLogEntity>("SessionLog");
export interface SessionLogEntity extends Entities.Entity {
    user?: Entities.Lite<UserEntity>;
    sessionStart?: string;
    sessionEnd?: string;
    sessionTimeOut?: boolean;
    userHostAddress?: string;
    userAgent?: string;
}

export module SessionLogPermission {
    export const TrackSession : PermissionSymbol = registerSymbol({ Type: "Permission", key: "SessionLogPermission.TrackSession" });
}

export enum TypeAllowed {
    None = "None" as any,
    DBReadUINone = "DBReadUINone" as any,
    Read = "Read" as any,
    DBModifyUINone = "DBModifyUINone" as any,
    DBModifyUIRead = "DBModifyUIRead" as any,
    Modify = "Modify" as any,
    DBCreateUINone = "DBCreateUINone" as any,
    DBCreateUIRead = "DBCreateUIRead" as any,
    DBCreateUIModify = "DBCreateUIModify" as any,
    Create = "Create" as any,
}
export const TypeAllowed_Type = new EnumType<TypeAllowed>("TypeAllowed", TypeAllowed);

export const TypeAllowedAndConditions_Type = new Type<TypeAllowedAndConditions>("TypeAllowedAndConditions");
export interface TypeAllowedAndConditions extends Entities.ModelEntity {
    fallback?: TypeAllowed;
    fallbackOrNone?: TypeAllowed;
    conditions?: Array<TypeConditionRule>;
}

export const TypeAllowedRule_Type = new Type<TypeAllowedRule>("TypeAllowedRule");
export interface TypeAllowedRule extends AllowedRule<Basics.TypeEntity, TypeAllowedAndConditions> {
    properties?: AuthThumbnail;
    operations?: AuthThumbnail;
    queries?: AuthThumbnail;
    availableConditions?: Array<ExBasics.TypeConditionSymbol>;
}

export const TypeConditionRule_Type = new Type<TypeConditionRule>("TypeConditionRule");
export interface TypeConditionRule extends Entities.EmbeddedEntity {
    typeCondition?: ExBasics.TypeConditionSymbol;
    allowed?: TypeAllowed;
}

export const TypeRulePack_Type = new Type<TypeRulePack>("TypeRulePack");
export interface TypeRulePack extends BaseRulePack<TypeAllowedRule> {
}

export const UserEntity_Type = new Type<UserEntity>("User");
export interface UserEntity extends Entities.Entity, Mailing.IEmailOwnerEntity, Basics.IUserEntity {
    userName?: string;
    passwordHash?: string;
    passwordSetDate?: string;
    passwordNeverExpires?: boolean;
    role?: RoleEntity;
    email?: string;
    cultureInfo?: ExBasics.CultureInfoEntity;
    anulationDate?: string;
    state?: UserState;
}

export module UserOperation {
    export const Create : Entities.ConstructSymbol_Simple<UserEntity> = registerSymbol({ Type: "Operation", key: "UserOperation.Create" });
    export const SaveNew : Entities.ExecuteSymbol<UserEntity> = registerSymbol({ Type: "Operation", key: "UserOperation.SaveNew" });
    export const Save : Entities.ExecuteSymbol<UserEntity> = registerSymbol({ Type: "Operation", key: "UserOperation.Save" });
    export const Enable : Entities.ExecuteSymbol<UserEntity> = registerSymbol({ Type: "Operation", key: "UserOperation.Enable" });
    export const Disable : Entities.ExecuteSymbol<UserEntity> = registerSymbol({ Type: "Operation", key: "UserOperation.Disable" });
    export const SetPassword : Entities.ExecuteSymbol<UserEntity> = registerSymbol({ Type: "Operation", key: "UserOperation.SetPassword" });
}

export enum UserState {
    New = "New" as any,
    Saved = "Saved" as any,
    Disabled = "Disabled" as any,
}
export const UserState_Type = new EnumType<UserState>("UserState", UserState);

export const UserTicketEntity_Type = new Type<UserTicketEntity>("UserTicket");
export interface UserTicketEntity extends Entities.Entity {
    user?: Entities.Lite<UserEntity>;
    ticket?: string;
    connectionDate?: string;
    device?: string;
}

