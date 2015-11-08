//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from 'Framework/Signum.React/Scripts/Reflection' 

import * as Entities from 'Framework/Signum.React/Scripts/Signum.Entities' 
export namespace Alerts {

    export const AlertEntity_Type = new Type<AlertEntity>("AlertEntity");
    export interface AlertEntity extends Entities.Entity {
        target?: Entities.Lite<Entities.Entity>;
        creationDate?: string;
        alertDate?: string;
        attendedDate?: string;
        title?: string;
        text?: string;
        createdBy?: Entities.Lite<Entities.Basics.IUserEntity>;
        attendedBy?: Entities.Lite<Entities.Basics.IUserEntity>;
        alertType?: AlertTypeEntity;
        state?: AlertState;
    }
    
    export module AlertMessage {
        export const Alert = new MessageKey("AlertMessage", "Alert");
        export const NewAlert = new MessageKey("AlertMessage", "NewAlert");
        export const Alerts = new MessageKey("AlertMessage", "Alerts");
        export const Alerts_Attended = new MessageKey("AlertMessage", "Alerts_Attended");
        export const Alerts_Future = new MessageKey("AlertMessage", "Alerts_Future");
        export const Alerts_NotAttended = new MessageKey("AlertMessage", "Alerts_NotAttended");
        export const CheckedAlerts = new MessageKey("AlertMessage", "CheckedAlerts");
        export const CreateAlert = new MessageKey("AlertMessage", "CreateAlert");
        export const FutureAlerts = new MessageKey("AlertMessage", "FutureAlerts");
        export const WarnedAlerts = new MessageKey("AlertMessage", "WarnedAlerts");
    }
    
    export module AlertOperation {
        export const CreateAlertFromEntity : Entities.ConstructSymbol_From<AlertEntity, Entities.Entity> = registerSymbol({ key: "AlertOperation.CreateAlertFromEntity" });
        export const SaveNew : Entities.ExecuteSymbol<AlertEntity> = registerSymbol({ key: "AlertOperation.SaveNew" });
        export const Save : Entities.ExecuteSymbol<AlertEntity> = registerSymbol({ key: "AlertOperation.Save" });
        export const Attend : Entities.ExecuteSymbol<AlertEntity> = registerSymbol({ key: "AlertOperation.Attend" });
        export const Unattend : Entities.ExecuteSymbol<AlertEntity> = registerSymbol({ key: "AlertOperation.Unattend" });
    }
    
    export enum AlertState {
        New,
        Saved,
        Attended,
    }
    export const AlertState_Type = new EnumType<AlertState>("AlertState", AlertState);
    
    export const AlertTypeEntity_Type = new Type<AlertTypeEntity>("AlertTypeEntity");
    export interface AlertTypeEntity extends Entities.Basics.SemiSymbol {
    }
    
    export module AlertTypeOperation {
        export const Save : Entities.ExecuteSymbol<AlertTypeEntity> = registerSymbol({ key: "AlertTypeOperation.Save" });
    }
    
}

export namespace Authorization {

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
        All,
        Mix,
        None,
    }
    export const AuthThumbnail_Type = new EnumType<AuthThumbnail>("AuthThumbnail", AuthThumbnail);
    
    export interface BaseRulePack<T> extends Entities.ModelEntity {
        role?: Entities.Lite<RoleEntity>;
        strategy?: string;
        type?: Entities.Basics.TypeEntity;
        rules?: Entities.MList<T>;
    }
    
    export module BasicPermission {
        export const AdminRules : PermissionSymbol = registerSymbol({ key: "BasicPermission.AdminRules" });
        export const AutomaticUpgradeOfProperties : PermissionSymbol = registerSymbol({ key: "BasicPermission.AutomaticUpgradeOfProperties" });
        export const AutomaticUpgradeOfQueries : PermissionSymbol = registerSymbol({ key: "BasicPermission.AutomaticUpgradeOfQueries" });
        export const AutomaticUpgradeOfOperations : PermissionSymbol = registerSymbol({ key: "BasicPermission.AutomaticUpgradeOfOperations" });
    }
    
    export const LastAuthRulesImportEntity_Type = new Type<LastAuthRulesImportEntity>("LastAuthRulesImportEntity");
    export interface LastAuthRulesImportEntity extends Entities.Entity {
        date?: string;
    }
    
    export enum MergeStrategy {
        Union,
        Intersection,
    }
    export const MergeStrategy_Type = new EnumType<MergeStrategy>("MergeStrategy", MergeStrategy);
    
    export enum OperationAllowed {
        None,
        DBOnly,
        Allow,
    }
    export const OperationAllowed_Type = new EnumType<OperationAllowed>("OperationAllowed", OperationAllowed);
    
    export const OperationAllowedRule_Type = new Type<OperationAllowedRule>("OperationAllowedRule");
    export interface OperationAllowedRule extends AllowedRuleCoerced<Entities.OperationSymbol, OperationAllowed> {
    }
    
    export const OperationRulePack_Type = new Type<OperationRulePack>("OperationRulePack");
    export interface OperationRulePack extends BaseRulePack<OperationAllowedRule> {
    }
    
    export const PasswordExpiresIntervalEntity_Type = new Type<PasswordExpiresIntervalEntity>("PasswordExpiresIntervalEntity");
    export interface PasswordExpiresIntervalEntity extends Entities.Entity {
        days?: number;
        daysWarning?: number;
        enabled?: boolean;
    }
    
    export module PasswordExpiresIntervalOperation {
        export const Save : Entities.ExecuteSymbol<PasswordExpiresIntervalEntity> = registerSymbol({ key: "PasswordExpiresIntervalOperation.Save" });
    }
    
    export const PermissionAllowedRule_Type = new Type<PermissionAllowedRule>("PermissionAllowedRule");
    export interface PermissionAllowedRule extends AllowedRule<PermissionSymbol, boolean> {
    }
    
    export const PermissionRulePack_Type = new Type<PermissionRulePack>("PermissionRulePack");
    export interface PermissionRulePack extends BaseRulePack<PermissionAllowedRule> {
    }
    
    export const PermissionSymbol_Type = new Type<PermissionSymbol>("PermissionSymbol");
    export interface PermissionSymbol extends Entities.Symbol {
    }
    
    export enum PropertyAllowed {
        None,
        Read,
        Modify,
    }
    export const PropertyAllowed_Type = new EnumType<PropertyAllowed>("PropertyAllowed", PropertyAllowed);
    
    export const PropertyAllowedRule_Type = new Type<PropertyAllowedRule>("PropertyAllowedRule");
    export interface PropertyAllowedRule extends AllowedRuleCoerced<Entities.Basics.PropertyRouteEntity, PropertyAllowed> {
    }
    
    export const PropertyRulePack_Type = new Type<PropertyRulePack>("PropertyRulePack");
    export interface PropertyRulePack extends BaseRulePack<PropertyAllowedRule> {
    }
    
    export const QueryAllowedRule_Type = new Type<QueryAllowedRule>("QueryAllowedRule");
    export interface QueryAllowedRule extends AllowedRuleCoerced<Entities.Basics.QueryEntity, boolean> {
    }
    
    export const QueryRulePack_Type = new Type<QueryRulePack>("QueryRulePack");
    export interface QueryRulePack extends BaseRulePack<QueryAllowedRule> {
    }
    
    export const ResetPasswordRequestEntity_Type = new Type<ResetPasswordRequestEntity>("ResetPasswordRequestEntity");
    export interface ResetPasswordRequestEntity extends Entities.Entity {
        code?: string;
        user?: UserEntity;
        requestDate?: string;
        lapsed?: boolean;
    }
    
    export const RoleEntity_Type = new Type<RoleEntity>("RoleEntity");
    export interface RoleEntity extends Entities.Entity {
        name?: string;
        mergeStrategy?: MergeStrategy;
        roles?: Entities.MList<Entities.Lite<RoleEntity>>;
    }
    
    export module RoleOperation {
        export const Save : Entities.ExecuteSymbol<RoleEntity> = registerSymbol({ key: "RoleOperation.Save" });
        export const Delete : Entities.DeleteSymbol<RoleEntity> = registerSymbol({ key: "RoleOperation.Delete" });
    }
    
    export module RoleQuery {
        export const RolesReferedBy = new MessageKey("RoleQuery", "RolesReferedBy");
    }
    
    export interface RuleEntity<R, A> extends Entities.Entity {
        role?: Entities.Lite<RoleEntity>;
        resource?: R;
        allowed?: A;
    }
    
    export const RuleOperationEntity_Type = new Type<RuleOperationEntity>("RuleOperationEntity");
    export interface RuleOperationEntity extends RuleEntity<Entities.OperationSymbol, OperationAllowed> {
    }
    
    export const RulePermissionEntity_Type = new Type<RulePermissionEntity>("RulePermissionEntity");
    export interface RulePermissionEntity extends RuleEntity<PermissionSymbol, boolean> {
    }
    
    export const RulePropertyEntity_Type = new Type<RulePropertyEntity>("RulePropertyEntity");
    export interface RulePropertyEntity extends RuleEntity<Entities.Basics.PropertyRouteEntity, PropertyAllowed> {
    }
    
    export const RuleQueryEntity_Type = new Type<RuleQueryEntity>("RuleQueryEntity");
    export interface RuleQueryEntity extends RuleEntity<Entities.Basics.QueryEntity, boolean> {
    }
    
    export const RuleTypeConditionEntity_Type = new Type<RuleTypeConditionEntity>("RuleTypeConditionEntity");
    export interface RuleTypeConditionEntity extends Entities.EmbeddedEntity {
        condition?: Basics.TypeConditionSymbol;
        allowed?: TypeAllowed;
    }
    
    export const RuleTypeEntity_Type = new Type<RuleTypeEntity>("RuleTypeEntity");
    export interface RuleTypeEntity extends RuleEntity<Entities.Basics.TypeEntity, TypeAllowed> {
        conditions?: Entities.MList<RuleTypeConditionEntity>;
    }
    
    export const SessionLogEntity_Type = new Type<SessionLogEntity>("SessionLogEntity");
    export interface SessionLogEntity extends Entities.Entity {
        user?: Entities.Lite<UserEntity>;
        sessionStart?: string;
        sessionEnd?: string;
        sessionTimeOut?: boolean;
        userHostAddress?: string;
        userAgent?: string;
    }
    
    export module SessionLogPermission {
        export const TrackSession : PermissionSymbol = registerSymbol({ key: "SessionLogPermission.TrackSession" });
    }
    
    export enum TypeAllowed {
        None,
        DBReadUINone = 4,
        Read,
        DBModifyUINone = 8,
        DBModifyUIRead,
        Modify,
        DBCreateUINone = 12,
        DBCreateUIRead,
        DBCreateUIModify,
        Create,
    }
    export const TypeAllowed_Type = new EnumType<TypeAllowed>("TypeAllowed", TypeAllowed);
    
    export const TypeAllowedAndConditions_Type = new Type<TypeAllowedAndConditions>("TypeAllowedAndConditions");
    export interface TypeAllowedAndConditions extends Entities.ModelEntity {
        fallback?: TypeAllowed;
        fallbackOrNone?: TypeAllowed;
        conditions?: Array<TypeConditionRule>;
    }
    
    export const TypeAllowedRule_Type = new Type<TypeAllowedRule>("TypeAllowedRule");
    export interface TypeAllowedRule extends AllowedRule<Entities.Basics.TypeEntity, TypeAllowedAndConditions> {
        properties?: AuthThumbnail;
        operations?: AuthThumbnail;
        queries?: AuthThumbnail;
        availableConditions?: Array<Basics.TypeConditionSymbol>;
    }
    
    export const TypeConditionRule_Type = new Type<TypeConditionRule>("TypeConditionRule");
    export interface TypeConditionRule extends Entities.EmbeddedEntity {
        typeCondition?: Basics.TypeConditionSymbol;
        allowed?: TypeAllowed;
    }
    
    export const TypeRulePack_Type = new Type<TypeRulePack>("TypeRulePack");
    export interface TypeRulePack extends BaseRulePack<TypeAllowedRule> {
    }
    
    export const UserEntity_Type = new Type<UserEntity>("UserEntity");
    export interface UserEntity extends Entities.Entity, Mailing.IEmailOwnerEntity, Entities.Basics.IUserEntity {
        userName?: string;
        passwordHash?: string;
        passwordSetDate?: string;
        passwordNeverExpires?: boolean;
        role?: RoleEntity;
        email?: string;
        cultureInfo?: Basics.CultureInfoEntity;
        anulationDate?: string;
        state?: UserState;
    }
    
    export module UserOperation {
        export const Create : Entities.ConstructSymbol_Simple<UserEntity> = registerSymbol({ key: "UserOperation.Create" });
        export const SaveNew : Entities.ExecuteSymbol<UserEntity> = registerSymbol({ key: "UserOperation.SaveNew" });
        export const Save : Entities.ExecuteSymbol<UserEntity> = registerSymbol({ key: "UserOperation.Save" });
        export const Enable : Entities.ExecuteSymbol<UserEntity> = registerSymbol({ key: "UserOperation.Enable" });
        export const Disable : Entities.ExecuteSymbol<UserEntity> = registerSymbol({ key: "UserOperation.Disable" });
        export const SetPassword : Entities.ExecuteSymbol<UserEntity> = registerSymbol({ key: "UserOperation.SetPassword" });
    }
    
    export enum UserState {
        New = -1,
        Saved,
        Disabled,
    }
    export const UserState_Type = new EnumType<UserState>("UserState", UserState);
    
    export const UserTicketEntity_Type = new Type<UserTicketEntity>("UserTicketEntity");
    export interface UserTicketEntity extends Entities.Entity {
        user?: Entities.Lite<UserEntity>;
        ticket?: string;
        connectionDate?: string;
        device?: string;
    }
    
}

export namespace Basics {

    export const CultureInfoEntity_Type = new Type<CultureInfoEntity>("CultureInfoEntity");
    export interface CultureInfoEntity extends Entities.Entity {
        name?: string;
        nativeName?: string;
        englishName?: string;
    }
    
    export module CultureInfoOperation {
        export const Save : Entities.ExecuteSymbol<CultureInfoEntity> = registerSymbol({ key: "CultureInfoOperation.Save" });
    }
    
    export const DateSpanEntity_Type = new Type<DateSpanEntity>("DateSpanEntity");
    export interface DateSpanEntity extends Entities.EmbeddedEntity {
        years?: number;
        months?: number;
        days?: number;
    }
    
    export const TypeConditionSymbol_Type = new Type<TypeConditionSymbol>("TypeConditionSymbol");
    export interface TypeConditionSymbol extends Entities.Symbol {
    }
    
}

export namespace Cache {

    export module CachePermission {
        export const ViewCache : Authorization.PermissionSymbol = registerSymbol({ key: "CachePermission.ViewCache" });
        export const InvalidateCache : Authorization.PermissionSymbol = registerSymbol({ key: "CachePermission.InvalidateCache" });
    }
    
}

export namespace Chart {

    export const ChartColorEntity_Type = new Type<ChartColorEntity>("ChartColorEntity");
    export interface ChartColorEntity extends Entities.Entity {
        related?: Entities.Lite<Entities.Entity>;
        color?: Entities.Basics.ColorEntity;
    }
    
    export const ChartColumnEntity_Type = new Type<ChartColumnEntity>("ChartColumnEntity");
    export interface ChartColumnEntity extends Entities.EmbeddedEntity {
        scriptColumn?: ChartScriptColumnEntity;
        token?: UserAssets.QueryTokenEntity;
        displayName?: string;
    }
    
    export enum ChartColumnType {
        Integer = 1,
        Real,
        Date = 4,
        DateTime = 8,
        String = 16,
        Lite = 32,
        Enum = 64,
        RealGroupable = 128,
        Groupable = 268435701,
        Magnitude = 268435587,
        Positionable = 268435663,
    }
    export const ChartColumnType_Type = new EnumType<ChartColumnType>("ChartColumnType", ChartColumnType);
    
    export module ChartMessage {
        export const _0CanOnlyBeCreatedFromTheChartWindow = new MessageKey("ChartMessage", "_0CanOnlyBeCreatedFromTheChartWindow");
        export const _0CanOnlyBeCreatedFromTheSearchWindow = new MessageKey("ChartMessage", "_0CanOnlyBeCreatedFromTheSearchWindow");
        export const Chart = new MessageKey("ChartMessage", "Chart");
        export const ChartToken = new MessageKey("ChartMessage", "ChartToken");
        export const Chart_ChartSettings = new MessageKey("ChartMessage", "Chart_ChartSettings");
        export const Chart_Dimension = new MessageKey("ChartMessage", "Chart_Dimension");
        export const Chart_Draw = new MessageKey("ChartMessage", "Chart_Draw");
        export const Chart_Group = new MessageKey("ChartMessage", "Chart_Group");
        export const Chart_Query0IsNotAllowed = new MessageKey("ChartMessage", "Chart_Query0IsNotAllowed");
        export const Chart_ToggleInfo = new MessageKey("ChartMessage", "Chart_ToggleInfo");
        export const EditScript = new MessageKey("ChartMessage", "EditScript");
        export const ColorsFor0 = new MessageKey("ChartMessage", "ColorsFor0");
        export const CreatePalette = new MessageKey("ChartMessage", "CreatePalette");
        export const MyCharts = new MessageKey("ChartMessage", "MyCharts");
        export const CreateNew = new MessageKey("ChartMessage", "CreateNew");
        export const EditUserChart = new MessageKey("ChartMessage", "EditUserChart");
        export const ViewPalette = new MessageKey("ChartMessage", "ViewPalette");
        export const ChartFor = new MessageKey("ChartMessage", "ChartFor");
        export const ChartOf0 = new MessageKey("ChartMessage", "ChartOf0");
        export const _0IsKeyBut1IsAnAggregate = new MessageKey("ChartMessage", "_0IsKeyBut1IsAnAggregate");
        export const _0ShouldBeAnAggregate = new MessageKey("ChartMessage", "_0ShouldBeAnAggregate");
        export const _0ShouldBeSet = new MessageKey("ChartMessage", "_0ShouldBeSet");
        export const _0ShouldBeNull = new MessageKey("ChartMessage", "_0ShouldBeNull");
        export const _0IsNot1 = new MessageKey("ChartMessage", "_0IsNot1");
        export const _0IsAnAggregateButTheChartIsNotGrouping = new MessageKey("ChartMessage", "_0IsAnAggregateButTheChartIsNotGrouping");
        export const _0IsNotOptional = new MessageKey("ChartMessage", "_0IsNotOptional");
        export const SavePalette = new MessageKey("ChartMessage", "SavePalette");
        export const NewPalette = new MessageKey("ChartMessage", "NewPalette");
        export const Data = new MessageKey("ChartMessage", "Data");
        export const ChooseABasePalette = new MessageKey("ChartMessage", "ChooseABasePalette");
        export const DeletePalette = new MessageKey("ChartMessage", "DeletePalette");
        export const Preview = new MessageKey("ChartMessage", "Preview");
    }
    
    export const ChartPaletteModel_Type = new Type<ChartPaletteModel>("ChartPaletteModel");
    export interface ChartPaletteModel extends Entities.ModelEntity {
        type?: Entities.Basics.TypeEntity;
        colors?: Entities.MList<ChartColorEntity>;
    }
    
    export const ChartParameterEntity_Type = new Type<ChartParameterEntity>("ChartParameterEntity");
    export interface ChartParameterEntity extends Entities.EmbeddedEntity {
        scriptParameter?: ChartScriptParameterEntity;
        name?: string;
        value?: string;
    }
    
    export enum ChartParameterType {
        Enum,
        Number,
        String,
    }
    export const ChartParameterType_Type = new EnumType<ChartParameterType>("ChartParameterType", ChartParameterType);
    
    export module ChartPermission {
        export const ViewCharting : Authorization.PermissionSymbol = registerSymbol({ key: "ChartPermission.ViewCharting" });
    }
    
    export const ChartScriptColumnEntity_Type = new Type<ChartScriptColumnEntity>("ChartScriptColumnEntity");
    export interface ChartScriptColumnEntity extends Entities.EmbeddedEntity {
        displayName?: string;
        isOptional?: boolean;
        columnType?: ChartColumnType;
        isGroupKey?: boolean;
    }
    
    export const ChartScriptEntity_Type = new Type<ChartScriptEntity>("ChartScriptEntity");
    export interface ChartScriptEntity extends Entities.Entity {
        name?: string;
        icon?: Entities.Lite<Files.FileEntity>;
        script?: string;
        groupBy?: GroupByChart;
        columns?: Entities.MList<ChartScriptColumnEntity>;
        parameters?: Entities.MList<ChartScriptParameterEntity>;
        columnsStructure?: string;
    }
    
    export module ChartScriptOperation {
        export const Save : Entities.ExecuteSymbol<ChartScriptEntity> = registerSymbol({ key: "ChartScriptOperation.Save" });
        export const Clone : Entities.ConstructSymbol_From<ChartScriptEntity, ChartScriptEntity> = registerSymbol({ key: "ChartScriptOperation.Clone" });
        export const Delete : Entities.DeleteSymbol<ChartScriptEntity> = registerSymbol({ key: "ChartScriptOperation.Delete" });
    }
    
    export const ChartScriptParameterEntity_Type = new Type<ChartScriptParameterEntity>("ChartScriptParameterEntity");
    export interface ChartScriptParameterEntity extends Entities.EmbeddedEntity {
        name?: string;
        type?: ChartParameterType;
        columnIndex?: number;
        valueDefinition?: string;
    }
    
    export enum GroupByChart {
        Always,
        Optional,
        Never,
    }
    export const GroupByChart_Type = new EnumType<GroupByChart>("GroupByChart", GroupByChart);
    
    export const UserChartEntity_Type = new Type<UserChartEntity>("UserChartEntity");
    export interface UserChartEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
        query?: Entities.Basics.QueryEntity;
        entityType?: Entities.Lite<Entities.Basics.TypeEntity>;
        owner?: Entities.Lite<Entities.Entity>;
        displayName?: string;
        chartScript?: ChartScriptEntity;
        parameters?: Entities.MList<ChartParameterEntity>;
        groupResults?: boolean;
        columns?: Entities.MList<ChartColumnEntity>;
        filters?: Entities.MList<UserQueries.QueryFilterEntity>;
        orders?: Entities.MList<UserQueries.QueryOrderEntity>;
        guid?: string;
        invalidator?: boolean;
    }
    
    export module UserChartOperation {
        export const Save : Entities.ExecuteSymbol<UserChartEntity> = registerSymbol({ key: "UserChartOperation.Save" });
        export const Delete : Entities.DeleteSymbol<UserChartEntity> = registerSymbol({ key: "UserChartOperation.Delete" });
    }
    
}

export namespace Dashboard {

    export const CountSearchControlPartEntity_Type = new Type<CountSearchControlPartEntity>("CountSearchControlPartEntity");
    export interface CountSearchControlPartEntity extends Entities.Entity, IPartEntity {
        userQueries?: Entities.MList<CountUserQueryElementEntity>;
        requiresTitle?: boolean;
    }
    
    export const CountUserQueryElementEntity_Type = new Type<CountUserQueryElementEntity>("CountUserQueryElementEntity");
    export interface CountUserQueryElementEntity extends Entities.EmbeddedEntity {
        label?: string;
        userQuery?: UserQueries.UserQueryEntity;
        href?: string;
    }
    
    export enum DashboardEmbedededInEntity {
        None,
        Top,
        Bottom,
    }
    export const DashboardEmbedededInEntity_Type = new EnumType<DashboardEmbedededInEntity>("DashboardEmbedededInEntity", DashboardEmbedededInEntity);
    
    export const DashboardEntity_Type = new Type<DashboardEntity>("DashboardEntity");
    export interface DashboardEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
        entityType?: Entities.Lite<Entities.Basics.TypeEntity>;
        embeddedInEntity?: DashboardEmbedededInEntity;
        owner?: Entities.Lite<Entities.Entity>;
        dashboardPriority?: number;
        autoRefreshPeriod?: number;
        displayName?: string;
        parts?: Entities.MList<PanelPartEntity>;
        guid?: string;
    }
    
    export module DashboardMessage {
        export const CreateNewPart = new MessageKey("DashboardMessage", "CreateNewPart");
        export const DashboardDN_TitleMustBeSpecifiedFor0 = new MessageKey("DashboardMessage", "DashboardDN_TitleMustBeSpecifiedFor0");
        export const CountSearchControlPartEntity = new MessageKey("DashboardMessage", "CountSearchControlPartEntity");
        export const CountUserQueryElement = new MessageKey("DashboardMessage", "CountUserQueryElement");
        export const Preview = new MessageKey("DashboardMessage", "Preview");
        export const _0Is1InstedOf2In3 = new MessageKey("DashboardMessage", "_0Is1InstedOf2In3");
        export const Part0IsTooLarge = new MessageKey("DashboardMessage", "Part0IsTooLarge");
        export const Part0OverlapsWith1 = new MessageKey("DashboardMessage", "Part0OverlapsWith1");
    }
    
    export module DashboardOperation {
        export const Create : Entities.ConstructSymbol_Simple<DashboardEntity> = registerSymbol({ key: "DashboardOperation.Create" });
        export const Save : Entities.ExecuteSymbol<DashboardEntity> = registerSymbol({ key: "DashboardOperation.Save" });
        export const Clone : Entities.ConstructSymbol_From<DashboardEntity, DashboardEntity> = registerSymbol({ key: "DashboardOperation.Clone" });
        export const Delete : Entities.DeleteSymbol<DashboardEntity> = registerSymbol({ key: "DashboardOperation.Delete" });
    }
    
    export module DashboardPermission {
        export const ViewDashboard : Authorization.PermissionSymbol = registerSymbol({ key: "DashboardPermission.ViewDashboard" });
    }
    
    export interface IPartEntity extends Entities.IEntity {
        requiresTitle?: boolean;
    }
    
    export const LinkElementEntity_Type = new Type<LinkElementEntity>("LinkElementEntity");
    export interface LinkElementEntity extends Entities.EmbeddedEntity {
        label?: string;
        link?: string;
    }
    
    export const LinkListPartEntity_Type = new Type<LinkListPartEntity>("LinkListPartEntity");
    export interface LinkListPartEntity extends Entities.Entity, IPartEntity {
        links?: Entities.MList<LinkElementEntity>;
        requiresTitle?: boolean;
    }
    
    export const PanelPartEntity_Type = new Type<PanelPartEntity>("PanelPartEntity");
    export interface PanelPartEntity extends Entities.EmbeddedEntity {
        title?: string;
        row?: number;
        startColumn?: number;
        columns?: number;
        style?: PanelStyle;
        content?: IPartEntity;
    }
    
    export enum PanelStyle {
        Default,
        Primary,
        Success,
        Info,
        Warning,
        Danger,
    }
    export const PanelStyle_Type = new EnumType<PanelStyle>("PanelStyle", PanelStyle);
    
    export const UserChartPartEntity_Type = new Type<UserChartPartEntity>("UserChartPartEntity");
    export interface UserChartPartEntity extends Entities.Entity, IPartEntity {
        userChart?: Chart.UserChartEntity;
        showData?: boolean;
        requiresTitle?: boolean;
    }
    
    export const UserQueryPartEntity_Type = new Type<UserQueryPartEntity>("UserQueryPartEntity");
    export interface UserQueryPartEntity extends Entities.Entity, IPartEntity {
        userQuery?: UserQueries.UserQueryEntity;
        requiresTitle?: boolean;
    }
    
}

export namespace DiffLog {

    export module DiffLogMessage {
        export const PreviousLog = new MessageKey("DiffLogMessage", "PreviousLog");
        export const NextLog = new MessageKey("DiffLogMessage", "NextLog");
        export const CurrentEntity = new MessageKey("DiffLogMessage", "CurrentEntity");
        export const NavigatesToThePreviousOperationLog = new MessageKey("DiffLogMessage", "NavigatesToThePreviousOperationLog");
        export const DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState = new MessageKey("DiffLogMessage", "DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState");
        export const StateWhenTheOperationStarted = new MessageKey("DiffLogMessage", "StateWhenTheOperationStarted");
        export const DifferenceBetweenInitialStateAndFinalState = new MessageKey("DiffLogMessage", "DifferenceBetweenInitialStateAndFinalState");
        export const StateWhenTheOperationFinished = new MessageKey("DiffLogMessage", "StateWhenTheOperationFinished");
        export const DifferenceBetweenFinalStateAndTheInitialStateOfNextLog = new MessageKey("DiffLogMessage", "DifferenceBetweenFinalStateAndTheInitialStateOfNextLog");
        export const NavigatesToTheNextOperationLog = new MessageKey("DiffLogMessage", "NavigatesToTheNextOperationLog");
        export const DifferenceBetweenFinalStateAndTheCurrentStateOfTheEntity = new MessageKey("DiffLogMessage", "DifferenceBetweenFinalStateAndTheCurrentStateOfTheEntity");
        export const NavigatesToTheCurrentEntity = new MessageKey("DiffLogMessage", "NavigatesToTheCurrentEntity");
    }
    
    export const DiffLogMixin_Type = new Type<DiffLogMixin>("DiffLogMixin");
    export interface DiffLogMixin extends Entities.MixinEntity {
        initialState?: string;
        finalState?: string;
    }
    
}

export namespace Disconnected {

    export const DisconnectedCreatedMixin_Type = new Type<DisconnectedCreatedMixin>("DisconnectedCreatedMixin");
    export interface DisconnectedCreatedMixin extends Entities.MixinEntity {
        disconnectedCreated?: boolean;
    }
    
    export const DisconnectedExportEntity_Type = new Type<DisconnectedExportEntity>("DisconnectedExportEntity");
    export interface DisconnectedExportEntity extends Entities.Entity {
        creationDate?: string;
        machine?: Entities.Lite<DisconnectedMachineEntity>;
        lock?: number;
        createDatabase?: number;
        createSchema?: number;
        disableForeignKeys?: number;
        copies?: Entities.MList<DisconnectedExportTableEntity>;
        enableForeignKeys?: number;
        reseedIds?: number;
        backupDatabase?: number;
        dropDatabase?: number;
        total?: number;
        state?: DisconnectedExportState;
        exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
    }
    
    export enum DisconnectedExportState {
        InProgress,
        Completed,
        Error,
    }
    export const DisconnectedExportState_Type = new EnumType<DisconnectedExportState>("DisconnectedExportState", DisconnectedExportState);
    
    export const DisconnectedExportTableEntity_Type = new Type<DisconnectedExportTableEntity>("DisconnectedExportTableEntity");
    export interface DisconnectedExportTableEntity extends Entities.EmbeddedEntity {
        type?: Entities.Lite<Entities.Basics.TypeEntity>;
        copyTable?: number;
        errors?: string;
    }
    
    export const DisconnectedImportEntity_Type = new Type<DisconnectedImportEntity>("DisconnectedImportEntity");
    export interface DisconnectedImportEntity extends Entities.Entity {
        creationDate?: string;
        machine?: Entities.Lite<DisconnectedMachineEntity>;
        restoreDatabase?: number;
        synchronizeSchema?: number;
        disableForeignKeys?: number;
        copies?: Entities.MList<DisconnectedImportTableEntity>;
        unlock?: number;
        enableForeignKeys?: number;
        dropDatabase?: number;
        total?: number;
        state?: DisconnectedImportState;
        exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
    }
    
    export enum DisconnectedImportState {
        InProgress,
        Completed,
        Error,
    }
    export const DisconnectedImportState_Type = new EnumType<DisconnectedImportState>("DisconnectedImportState", DisconnectedImportState);
    
    export const DisconnectedImportTableEntity_Type = new Type<DisconnectedImportTableEntity>("DisconnectedImportTableEntity");
    export interface DisconnectedImportTableEntity extends Entities.EmbeddedEntity {
        type?: Entities.Lite<Entities.Basics.TypeEntity>;
        copyTable?: number;
        disableForeignKeys?: boolean;
        insertedRows?: number;
        updatedRows?: number;
        insertedOrUpdated?: number;
    }
    
    export const DisconnectedMachineEntity_Type = new Type<DisconnectedMachineEntity>("DisconnectedMachineEntity");
    export interface DisconnectedMachineEntity extends Entities.Entity {
        creationDate?: string;
        machineName?: string;
        state?: DisconnectedMachineState;
        seedMin?: number;
        seedMax?: number;
    }
    
    export module DisconnectedMachineOperation {
        export const Save : Entities.ExecuteSymbol<DisconnectedMachineEntity> = registerSymbol({ key: "DisconnectedMachineOperation.Save" });
        export const UnsafeUnlock : Entities.ExecuteSymbol<DisconnectedMachineEntity> = registerSymbol({ key: "DisconnectedMachineOperation.UnsafeUnlock" });
        export const FixImport : Entities.ConstructSymbol_From<DisconnectedImportEntity, DisconnectedMachineEntity> = registerSymbol({ key: "DisconnectedMachineOperation.FixImport" });
    }
    
    export enum DisconnectedMachineState {
        Connected,
        Disconnected,
        Faulted,
        Fixed,
    }
    export const DisconnectedMachineState_Type = new EnumType<DisconnectedMachineState>("DisconnectedMachineState", DisconnectedMachineState);
    
    export module DisconnectedMessage {
        export const NotAllowedToSave0WhileOffline = new MessageKey("DisconnectedMessage", "NotAllowedToSave0WhileOffline");
        export const The0WithId12IsLockedBy3 = new MessageKey("DisconnectedMessage", "The0WithId12IsLockedBy3");
        export const Imports = new MessageKey("DisconnectedMessage", "Imports");
        export const Exports = new MessageKey("DisconnectedMessage", "Exports");
        export const _0OverlapsWith1 = new MessageKey("DisconnectedMessage", "_0OverlapsWith1");
    }
    
    export const DisconnectedSubsetMixin_Type = new Type<DisconnectedSubsetMixin>("DisconnectedSubsetMixin");
    export interface DisconnectedSubsetMixin extends Entities.MixinEntity {
        lastOnlineTicks?: number;
        disconnectedMachine?: Entities.Lite<DisconnectedMachineEntity>;
    }
    
}

export namespace Excel {

    export module ExcelMessage {
        export const Data = new MessageKey("ExcelMessage", "Data");
        export const Download = new MessageKey("ExcelMessage", "Download");
        export const Excel2007Spreadsheet = new MessageKey("ExcelMessage", "Excel2007Spreadsheet");
        export const Administer = new MessageKey("ExcelMessage", "Administer");
        export const ExcelReport = new MessageKey("ExcelMessage", "ExcelReport");
        export const ExcelTemplateMustHaveExtensionXLSXandCurrentOneHas0 = new MessageKey("ExcelMessage", "ExcelTemplateMustHaveExtensionXLSXandCurrentOneHas0");
        export const FindLocationFoExcelReport = new MessageKey("ExcelMessage", "FindLocationFoExcelReport");
        export const Reports = new MessageKey("ExcelMessage", "Reports");
        export const TheExcelTemplateHasAColumn0NotPresentInTheFindWindow = new MessageKey("ExcelMessage", "TheExcelTemplateHasAColumn0NotPresentInTheFindWindow");
        export const ThereAreNoResultsToWrite = new MessageKey("ExcelMessage", "ThereAreNoResultsToWrite");
        export const CreateNew = new MessageKey("ExcelMessage", "CreateNew");
    }
    
    export const ExcelReportEntity_Type = new Type<ExcelReportEntity>("ExcelReportEntity");
    export interface ExcelReportEntity extends Entities.Entity {
        query?: Entities.Basics.QueryEntity;
        displayName?: string;
        file?: Files.EmbeddedFileEntity;
    }
    
    export module ExcelReportOperation {
        export const Save : Entities.ExecuteSymbol<ExcelReportEntity> = registerSymbol({ key: "ExcelReportOperation.Save" });
        export const Delete : Entities.DeleteSymbol<ExcelReportEntity> = registerSymbol({ key: "ExcelReportOperation.Delete" });
    }
    
}

export namespace External {

    export enum DayOfWeek {
        Sunday,
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
    }
    export const DayOfWeek_Type = new EnumType<DayOfWeek>("DayOfWeek", DayOfWeek);
    
    export enum SmtpDeliveryFormat {
        SevenBit,
        International,
    }
    export const SmtpDeliveryFormat_Type = new EnumType<SmtpDeliveryFormat>("SmtpDeliveryFormat", SmtpDeliveryFormat);
    
    export enum SmtpDeliveryMethod {
        Network,
        SpecifiedPickupDirectory,
        PickupDirectoryFromIis,
    }
    export const SmtpDeliveryMethod_Type = new EnumType<SmtpDeliveryMethod>("SmtpDeliveryMethod", SmtpDeliveryMethod);
    
}

export namespace Files {

    export const EmbeddedFileEntity_Type = new Type<EmbeddedFileEntity>("EmbeddedFileEntity");
    export interface EmbeddedFileEntity extends Entities.EmbeddedEntity {
        fileName?: string;
        binaryFile?: string;
        fullWebPath?: string;
    }
    
    export const EmbeddedFilePathEntity_Type = new Type<EmbeddedFilePathEntity>("EmbeddedFilePathEntity");
    export interface EmbeddedFilePathEntity extends Entities.EmbeddedEntity {
        fileName?: string;
        binaryFile?: string;
        fileLength?: number;
        fileLengthString?: string;
        sufix?: string;
        calculatedDirectory?: string;
        fileType?: FileTypeSymbol;
        fullPhysicalPath?: string;
        fullWebPath?: string;
    }
    
    export const FileEntity_Type = new Type<FileEntity>("FileEntity");
    export interface FileEntity extends Entities.ImmutableEntity {
        fileName?: string;
        hash?: string;
        binaryFile?: string;
        fullWebPath?: string;
    }
    
    export module FileMessage {
        export const DownloadFile = new MessageKey("FileMessage", "DownloadFile");
        export const ErrorSavingFile = new MessageKey("FileMessage", "ErrorSavingFile");
        export const FileTypes = new MessageKey("FileMessage", "FileTypes");
        export const Open = new MessageKey("FileMessage", "Open");
        export const OpeningHasNotDefaultImplementationFor0 = new MessageKey("FileMessage", "OpeningHasNotDefaultImplementationFor0");
        export const WebDownload = new MessageKey("FileMessage", "WebDownload");
        export const WebImage = new MessageKey("FileMessage", "WebImage");
        export const Remove = new MessageKey("FileMessage", "Remove");
        export const SavingHasNotDefaultImplementationFor0 = new MessageKey("FileMessage", "SavingHasNotDefaultImplementationFor0");
        export const SelectFile = new MessageKey("FileMessage", "SelectFile");
        export const ViewFile = new MessageKey("FileMessage", "ViewFile");
        export const ViewingHasNotDefaultImplementationFor0 = new MessageKey("FileMessage", "ViewingHasNotDefaultImplementationFor0");
        export const OnlyOneFileIsSupported = new MessageKey("FileMessage", "OnlyOneFileIsSupported");
    }
    
    export const FilePathEntity_Type = new Type<FilePathEntity>("FilePathEntity");
    export interface FilePathEntity extends Entities.Patterns.LockableEntity {
        creationDate?: string;
        fileName?: string;
        binaryFile?: string;
        fileLength?: number;
        fileLengthString?: string;
        sufix?: string;
        calculatedDirectory?: string;
        fileType?: FileTypeSymbol;
        fullPhysicalPath?: string;
        fullWebPath?: string;
    }
    
    export module FilePathOperation {
        export const Save : Entities.ExecuteSymbol<FilePathEntity> = registerSymbol({ key: "FilePathOperation.Save" });
    }
    
    export const FileTypeSymbol_Type = new Type<FileTypeSymbol>("FileTypeSymbol");
    export interface FileTypeSymbol extends Entities.Symbol {
    }
    
}

export namespace Help {

    export const AppendixHelpEntity_Type = new Type<AppendixHelpEntity>("AppendixHelpEntity");
    export interface AppendixHelpEntity extends Entities.Entity {
        uniqueName?: string;
        culture?: Basics.CultureInfoEntity;
        title?: string;
        description?: string;
    }
    
    export module AppendixHelpOperation {
        export const Save : Entities.ExecuteSymbol<AppendixHelpEntity> = registerSymbol({ key: "AppendixHelpOperation.Save" });
    }
    
    export const EntityHelpEntity_Type = new Type<EntityHelpEntity>("EntityHelpEntity");
    export interface EntityHelpEntity extends Entities.Entity {
        type?: Entities.Basics.TypeEntity;
        culture?: Basics.CultureInfoEntity;
        description?: string;
        properties?: Entities.MList<PropertyRouteHelpEntity>;
        operations?: Entities.MList<OperationHelpEntity>;
        queries?: Entities.MList<QueryHelpEntity>;
        isEmpty?: boolean;
    }
    
    export module EntityHelpOperation {
        export const Save : Entities.ExecuteSymbol<EntityHelpEntity> = registerSymbol({ key: "EntityHelpOperation.Save" });
    }
    
    export module HelpKindMessage {
        export const HisMainFunctionIsTo0 = new MessageKey("HelpKindMessage", "HisMainFunctionIsTo0");
        export const RelateOtherEntities = new MessageKey("HelpKindMessage", "RelateOtherEntities");
        export const ClassifyOtherEntities = new MessageKey("HelpKindMessage", "ClassifyOtherEntities");
        export const StoreInformationSharedByOtherEntities = new MessageKey("HelpKindMessage", "StoreInformationSharedByOtherEntities");
        export const StoreInformationOnItsOwn = new MessageKey("HelpKindMessage", "StoreInformationOnItsOwn");
        export const StorePartOfTheInformationOfAnotherEntity = new MessageKey("HelpKindMessage", "StorePartOfTheInformationOfAnotherEntity");
        export const StorePartsOfInformationSharedByDifferentEntities = new MessageKey("HelpKindMessage", "StorePartsOfInformationSharedByDifferentEntities");
        export const AutomaticallyByTheSystem = new MessageKey("HelpKindMessage", "AutomaticallyByTheSystem");
        export const AndIsRarelyCreatedOrModified = new MessageKey("HelpKindMessage", "AndIsRarelyCreatedOrModified");
        export const AndAreFrequentlyCreatedOrModified = new MessageKey("HelpKindMessage", "AndAreFrequentlyCreatedOrModified");
    }
    
    export module HelpMessage {
        export const _0IsA1 = new MessageKey("HelpMessage", "_0IsA1");
        export const _0IsA1AndShows2 = new MessageKey("HelpMessage", "_0IsA1AndShows2");
        export const _0IsACalculated1 = new MessageKey("HelpMessage", "_0IsACalculated1");
        export const _0IsACollectionOfElements1 = new MessageKey("HelpMessage", "_0IsACollectionOfElements1");
        export const Amount = new MessageKey("HelpMessage", "Amount");
        export const Any = new MessageKey("HelpMessage", "Any");
        export const Appendices = new MessageKey("HelpMessage", "Appendices");
        export const Buscador = new MessageKey("HelpMessage", "Buscador");
        export const Call0Over1OfThe2 = new MessageKey("HelpMessage", "Call0Over1OfThe2");
        export const Character = new MessageKey("HelpMessage", "Character");
        export const Check = new MessageKey("HelpMessage", "Check");
        export const ConstructsANew0 = new MessageKey("HelpMessage", "ConstructsANew0");
        export const Date = new MessageKey("HelpMessage", "Date");
        export const DateTime = new MessageKey("HelpMessage", "DateTime");
        export const ExpressedIn = new MessageKey("HelpMessage", "ExpressedIn");
        export const From0OfThe1 = new MessageKey("HelpMessage", "From0OfThe1");
        export const FromMany0 = new MessageKey("HelpMessage", "FromMany0");
        export const Help = new MessageKey("HelpMessage", "Help");
        export const HelpNotLoaded = new MessageKey("HelpMessage", "HelpNotLoaded");
        export const Integer = new MessageKey("HelpMessage", "Integer");
        export const Key0NotFound = new MessageKey("HelpMessage", "Key0NotFound");
        export const OfThe0 = new MessageKey("HelpMessage", "OfThe0");
        export const OrNull = new MessageKey("HelpMessage", "OrNull");
        export const Property0NotExistsInType1 = new MessageKey("HelpMessage", "Property0NotExistsInType1");
        export const QueryOf0 = new MessageKey("HelpMessage", "QueryOf0");
        export const RemovesThe0FromTheDatabase = new MessageKey("HelpMessage", "RemovesThe0FromTheDatabase");
        export const Should = new MessageKey("HelpMessage", "Should");
        export const String = new MessageKey("HelpMessage", "String");
        export const The0 = new MessageKey("HelpMessage", "The0");
        export const TheDatabaseVersion = new MessageKey("HelpMessage", "TheDatabaseVersion");
        export const TheProperty0 = new MessageKey("HelpMessage", "TheProperty0");
        export const Value = new MessageKey("HelpMessage", "Value");
        export const ValueLike0 = new MessageKey("HelpMessage", "ValueLike0");
        export const YourVersion = new MessageKey("HelpMessage", "YourVersion");
        export const _0IsThePrimaryKeyOf1OfType2 = new MessageKey("HelpMessage", "_0IsThePrimaryKeyOf1OfType2");
        export const In0 = new MessageKey("HelpMessage", "In0");
        export const Entities = new MessageKey("HelpMessage", "Entities");
        export const SearchText = new MessageKey("HelpMessage", "SearchText");
    }
    
    export module HelpPermissions {
        export const ViewHelp : Authorization.PermissionSymbol = registerSymbol({ key: "HelpPermissions.ViewHelp" });
    }
    
    export module HelpSearchMessage {
        export const Search = new MessageKey("HelpSearchMessage", "Search");
        export const _0ResultsFor1In2 = new MessageKey("HelpSearchMessage", "_0ResultsFor1In2");
        export const Results = new MessageKey("HelpSearchMessage", "Results");
    }
    
    export module HelpSyntaxMessage {
        export const BoldText = new MessageKey("HelpSyntaxMessage", "BoldText");
        export const ItalicText = new MessageKey("HelpSyntaxMessage", "ItalicText");
        export const UnderlineText = new MessageKey("HelpSyntaxMessage", "UnderlineText");
        export const StriketroughText = new MessageKey("HelpSyntaxMessage", "StriketroughText");
        export const LinkToEntity = new MessageKey("HelpSyntaxMessage", "LinkToEntity");
        export const LinkToProperty = new MessageKey("HelpSyntaxMessage", "LinkToProperty");
        export const LinkToQuery = new MessageKey("HelpSyntaxMessage", "LinkToQuery");
        export const LinkToOperation = new MessageKey("HelpSyntaxMessage", "LinkToOperation");
        export const LinkToNamespace = new MessageKey("HelpSyntaxMessage", "LinkToNamespace");
        export const ExernalLink = new MessageKey("HelpSyntaxMessage", "ExernalLink");
        export const LinksAllowAnExtraParameterForTheText = new MessageKey("HelpSyntaxMessage", "LinksAllowAnExtraParameterForTheText");
        export const Example = new MessageKey("HelpSyntaxMessage", "Example");
        export const UnorderedListItem = new MessageKey("HelpSyntaxMessage", "UnorderedListItem");
        export const OtherItem = new MessageKey("HelpSyntaxMessage", "OtherItem");
        export const OrderedListItem = new MessageKey("HelpSyntaxMessage", "OrderedListItem");
        export const TitleLevel = new MessageKey("HelpSyntaxMessage", "TitleLevel");
        export const Title = new MessageKey("HelpSyntaxMessage", "Title");
        export const Images = new MessageKey("HelpSyntaxMessage", "Images");
        export const Texts = new MessageKey("HelpSyntaxMessage", "Texts");
        export const Links = new MessageKey("HelpSyntaxMessage", "Links");
        export const Lists = new MessageKey("HelpSyntaxMessage", "Lists");
        export const InsertImage = new MessageKey("HelpSyntaxMessage", "InsertImage");
        export const Options = new MessageKey("HelpSyntaxMessage", "Options");
        export const Edit = new MessageKey("HelpSyntaxMessage", "Edit");
        export const Save = new MessageKey("HelpSyntaxMessage", "Save");
        export const Syntax = new MessageKey("HelpSyntaxMessage", "Syntax");
        export const TranslateFrom = new MessageKey("HelpSyntaxMessage", "TranslateFrom");
    }
    
    export const NamespaceHelpEntity_Type = new Type<NamespaceHelpEntity>("NamespaceHelpEntity");
    export interface NamespaceHelpEntity extends Entities.Entity {
        name?: string;
        culture?: Basics.CultureInfoEntity;
        title?: string;
        description?: string;
    }
    
    export module NamespaceHelpOperation {
        export const Save : Entities.ExecuteSymbol<NamespaceHelpEntity> = registerSymbol({ key: "NamespaceHelpOperation.Save" });
    }
    
    export const OperationHelpEntity_Type = new Type<OperationHelpEntity>("OperationHelpEntity");
    export interface OperationHelpEntity extends Entities.Entity {
        operation?: Entities.OperationSymbol;
        culture?: Basics.CultureInfoEntity;
        description?: string;
    }
    
    export module OperationHelpOperation {
        export const Save : Entities.ExecuteSymbol<OperationHelpEntity> = registerSymbol({ key: "OperationHelpOperation.Save" });
    }
    
    export const PropertyRouteHelpEntity_Type = new Type<PropertyRouteHelpEntity>("PropertyRouteHelpEntity");
    export interface PropertyRouteHelpEntity extends Entities.EmbeddedEntity {
        property?: Entities.Basics.PropertyRouteEntity;
        description?: string;
    }
    
    export const QueryColumnHelpEntity_Type = new Type<QueryColumnHelpEntity>("QueryColumnHelpEntity");
    export interface QueryColumnHelpEntity extends Entities.EmbeddedEntity {
        columnName?: string;
        description?: string;
    }
    
    export const QueryHelpEntity_Type = new Type<QueryHelpEntity>("QueryHelpEntity");
    export interface QueryHelpEntity extends Entities.Entity {
        query?: Entities.Basics.QueryEntity;
        culture?: Basics.CultureInfoEntity;
        description?: string;
        columns?: Entities.MList<QueryColumnHelpEntity>;
        isEmpty?: boolean;
    }
    
    export module QueryHelpOperation {
        export const Save : Entities.ExecuteSymbol<QueryHelpEntity> = registerSymbol({ key: "QueryHelpOperation.Save" });
    }
    
}

export namespace Isolation {

    export const IsolationEntity_Type = new Type<IsolationEntity>("IsolationEntity");
    export interface IsolationEntity extends Entities.Entity {
        name?: string;
    }
    
    export module IsolationMessage {
        export const Entity0HasIsolation1ButCurrentIsolationIs2 = new MessageKey("IsolationMessage", "Entity0HasIsolation1ButCurrentIsolationIs2");
        export const SelectAnIsolation = new MessageKey("IsolationMessage", "SelectAnIsolation");
        export const Entity0HasIsolation1ButEntity2HasIsolation3 = new MessageKey("IsolationMessage", "Entity0HasIsolation1ButEntity2HasIsolation3");
    }
    
    export const IsolationMixin_Type = new Type<IsolationMixin>("IsolationMixin");
    export interface IsolationMixin extends Entities.MixinEntity {
        isolation?: Entities.Lite<IsolationEntity>;
    }
    
    export module IsolationOperation {
        export const Save : Entities.ExecuteSymbol<IsolationEntity> = registerSymbol({ key: "IsolationOperation.Save" });
    }
    
}

export namespace Mailing {

    export module AsyncEmailSenderPermission {
        export const ViewAsyncEmailSenderPanel : Authorization.PermissionSymbol = registerSymbol({ key: "AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel" });
    }
    
    export enum CertFileType {
        CertFile,
        SignedFile,
    }
    export const CertFileType_Type = new EnumType<CertFileType>("CertFileType", CertFileType);
    
    export const ClientCertificationFileEntity_Type = new Type<ClientCertificationFileEntity>("ClientCertificationFileEntity");
    export interface ClientCertificationFileEntity extends Entities.EmbeddedEntity {
        fullFilePath?: string;
        certFileType?: CertFileType;
    }
    
    export const EmailAddressEntity_Type = new Type<EmailAddressEntity>("EmailAddressEntity");
    export interface EmailAddressEntity extends Entities.EmbeddedEntity {
        emailOwner?: Entities.Lite<IEmailOwnerEntity>;
        emailAddress?: string;
        displayName?: string;
    }
    
    export const EmailAttachmentEntity_Type = new Type<EmailAttachmentEntity>("EmailAttachmentEntity");
    export interface EmailAttachmentEntity extends Entities.EmbeddedEntity {
        type?: EmailAttachmentType;
        file?: Files.EmbeddedFilePathEntity;
        contentId?: string;
    }
    
    export enum EmailAttachmentType {
        Attachment,
        LinkedResource,
    }
    export const EmailAttachmentType_Type = new EnumType<EmailAttachmentType>("EmailAttachmentType", EmailAttachmentType);
    
    export const EmailConfigurationEntity_Type = new Type<EmailConfigurationEntity>("EmailConfigurationEntity");
    export interface EmailConfigurationEntity extends Entities.EmbeddedEntity {
        defaultCulture?: Basics.CultureInfoEntity;
        urlLeft?: string;
        sendEmails?: boolean;
        reciveEmails?: boolean;
        overrideEmailAddress?: string;
        avoidSendingEmailsOlderThan?: number;
        chunkSizeSendingEmails?: number;
        maxEmailSendRetries?: number;
        asyncSenderPeriod?: number;
    }
    
    export module EmailFileType {
        export const Attachment : Files.FileTypeSymbol = registerSymbol({ key: "EmailFileType.Attachment" });
    }
    
    export const EmailMasterTemplateEntity_Type = new Type<EmailMasterTemplateEntity>("EmailMasterTemplateEntity");
    export interface EmailMasterTemplateEntity extends Entities.Entity {
        name?: string;
        messages?: Entities.MList<EmailMasterTemplateMessageEntity>;
    }
    
    export const EmailMasterTemplateMessageEntity_Type = new Type<EmailMasterTemplateMessageEntity>("EmailMasterTemplateMessageEntity");
    export interface EmailMasterTemplateMessageEntity extends Entities.EmbeddedEntity {
        masterTemplate?: EmailMasterTemplateEntity;
        cultureInfo?: Basics.CultureInfoEntity;
        text?: string;
    }
    
    export module EmailMasterTemplateOperation {
        export const Create : Entities.ConstructSymbol_Simple<EmailMasterTemplateEntity> = registerSymbol({ key: "EmailMasterTemplateOperation.Create" });
        export const Save : Entities.ExecuteSymbol<EmailMasterTemplateEntity> = registerSymbol({ key: "EmailMasterTemplateOperation.Save" });
    }
    
    export const EmailMessageEntity_Type = new Type<EmailMessageEntity>("EmailMessageEntity");
    export interface EmailMessageEntity extends Entities.Entity, Processes.IProcessLineDataEntity {
        recipients?: Entities.MList<EmailRecipientEntity>;
        target?: Entities.Lite<Entities.Entity>;
        from?: EmailAddressEntity;
        template?: Entities.Lite<EmailTemplateEntity>;
        creationDate?: string;
        sent?: string;
        receptionNotified?: string;
        subject?: string;
        body?: string;
        bodyHash?: string;
        isBodyHtml?: boolean;
        exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
        state?: EmailMessageState;
        uniqueIdentifier?: string;
        editableMessage?: boolean;
        package?: Entities.Lite<EmailPackageEntity>;
        processIdentifier?: string;
        sendRetries?: number;
        attachments?: Entities.MList<EmailAttachmentEntity>;
    }
    
    export module EmailMessageMessage {
        export const TheEmailMessageCannotBeSentFromState0 = new MessageKey("EmailMessageMessage", "TheEmailMessageCannotBeSentFromState0");
        export const Message = new MessageKey("EmailMessageMessage", "Message");
        export const Messages = new MessageKey("EmailMessageMessage", "Messages");
        export const RemainingMessages = new MessageKey("EmailMessageMessage", "RemainingMessages");
        export const ExceptionMessages = new MessageKey("EmailMessageMessage", "ExceptionMessages");
        export const DefaultFromIsMandatory = new MessageKey("EmailMessageMessage", "DefaultFromIsMandatory");
        export const From = new MessageKey("EmailMessageMessage", "From");
        export const To = new MessageKey("EmailMessageMessage", "To");
        export const Attachments = new MessageKey("EmailMessageMessage", "Attachments");
    }
    
    export module EmailMessageOperation {
        export const Save : Entities.ExecuteSymbol<EmailMessageEntity> = registerSymbol({ key: "EmailMessageOperation.Save" });
        export const ReadyToSend : Entities.ExecuteSymbol<EmailMessageEntity> = registerSymbol({ key: "EmailMessageOperation.ReadyToSend" });
        export const Send : Entities.ExecuteSymbol<EmailMessageEntity> = registerSymbol({ key: "EmailMessageOperation.Send" });
        export const ReSend : Entities.ConstructSymbol_From<EmailMessageEntity, EmailMessageEntity> = registerSymbol({ key: "EmailMessageOperation.ReSend" });
        export const ReSendEmails : Entities.ConstructSymbol_FromMany<Processes.ProcessEntity, EmailMessageEntity> = registerSymbol({ key: "EmailMessageOperation.ReSendEmails" });
        export const CreateMail : Entities.ConstructSymbol_Simple<EmailMessageEntity> = registerSymbol({ key: "EmailMessageOperation.CreateMail" });
        export const CreateMailFromTemplate : Entities.ConstructSymbol_From<EmailMessageEntity, EmailTemplateEntity> = registerSymbol({ key: "EmailMessageOperation.CreateMailFromTemplate" });
        export const Delete : Entities.DeleteSymbol<EmailMessageEntity> = registerSymbol({ key: "EmailMessageOperation.Delete" });
    }
    
    export module EmailMessageProcess {
        export const SendEmails : Processes.ProcessAlgorithmSymbol = registerSymbol({ key: "EmailMessageProcess.SendEmails" });
    }
    
    export enum EmailMessageState {
        Created,
        Draft,
        ReadyToSend,
        RecruitedForSending,
        Sent,
        SentException,
        ReceptionNotified,
        Received,
        Outdated,
    }
    export const EmailMessageState_Type = new EnumType<EmailMessageState>("EmailMessageState", EmailMessageState);
    
    export const EmailPackageEntity_Type = new Type<EmailPackageEntity>("EmailPackageEntity");
    export interface EmailPackageEntity extends Entities.Entity, Processes.IProcessDataEntity {
        name?: string;
    }
    
    export const EmailReceptionInfoEntity_Type = new Type<EmailReceptionInfoEntity>("EmailReceptionInfoEntity");
    export interface EmailReceptionInfoEntity extends Entities.EmbeddedEntity {
        uniqueId?: string;
        reception?: Entities.Lite<Pop3ReceptionEntity>;
        rawContent?: string;
        sentDate?: string;
        receivedDate?: string;
        deletionDate?: string;
    }
    
    export const EmailReceptionMixin_Type = new Type<EmailReceptionMixin>("EmailReceptionMixin");
    export interface EmailReceptionMixin extends Entities.MixinEntity {
        receptionInfo?: EmailReceptionInfoEntity;
    }
    
    export const EmailRecipientEntity_Type = new Type<EmailRecipientEntity>("EmailRecipientEntity");
    export interface EmailRecipientEntity extends EmailAddressEntity {
        kind?: EmailRecipientKind;
    }
    
    export enum EmailRecipientKind {
        To,
        Cc,
        Bcc,
    }
    export const EmailRecipientKind_Type = new EnumType<EmailRecipientKind>("EmailRecipientKind", EmailRecipientKind);
    
    export const EmailTemplateContactEntity_Type = new Type<EmailTemplateContactEntity>("EmailTemplateContactEntity");
    export interface EmailTemplateContactEntity extends Entities.EmbeddedEntity {
        token?: UserAssets.QueryTokenEntity;
        emailAddress?: string;
        displayName?: string;
    }
    
    export const EmailTemplateEntity_Type = new Type<EmailTemplateEntity>("EmailTemplateEntity");
    export interface EmailTemplateEntity extends Entities.Entity {
        name?: string;
        editableMessage?: boolean;
        disableAuthorization?: boolean;
        query?: Entities.Basics.QueryEntity;
        systemEmail?: SystemEmailEntity;
        sendDifferentMessages?: boolean;
        from?: EmailTemplateContactEntity;
        recipients?: Entities.MList<EmailTemplateRecipientEntity>;
        masterTemplate?: Entities.Lite<EmailMasterTemplateEntity>;
        isBodyHtml?: boolean;
        messages?: Entities.MList<EmailTemplateMessageEntity>;
        active?: boolean;
        startDate?: string;
        endDate?: string;
    }
    
    export module EmailTemplateMessage {
        export const EndDateMustBeHigherThanStartDate = new MessageKey("EmailTemplateMessage", "EndDateMustBeHigherThanStartDate");
        export const ThereAreNoMessagesForTheTemplate = new MessageKey("EmailTemplateMessage", "ThereAreNoMessagesForTheTemplate");
        export const ThereMustBeAMessageFor0 = new MessageKey("EmailTemplateMessage", "ThereMustBeAMessageFor0");
        export const TheresMoreThanOneMessageForTheSameLanguage = new MessageKey("EmailTemplateMessage", "TheresMoreThanOneMessageForTheSameLanguage");
        export const TheTextMustContain0IndicatingReplacementPoint = new MessageKey("EmailTemplateMessage", "TheTextMustContain0IndicatingReplacementPoint");
        export const TheTemplateIsAlreadyActive = new MessageKey("EmailTemplateMessage", "TheTemplateIsAlreadyActive");
        export const TheTemplateIsAlreadyInactive = new MessageKey("EmailTemplateMessage", "TheTemplateIsAlreadyInactive");
        export const SystemEmailShouldBeSetToAccessModel0 = new MessageKey("EmailTemplateMessage", "SystemEmailShouldBeSetToAccessModel0");
        export const NewCulture = new MessageKey("EmailTemplateMessage", "NewCulture");
        export const TokenOrEmailAddressMustBeSet = new MessageKey("EmailTemplateMessage", "TokenOrEmailAddressMustBeSet");
        export const TokenAndEmailAddressCanNotBeSetAtTheSameTime = new MessageKey("EmailTemplateMessage", "TokenAndEmailAddressCanNotBeSetAtTheSameTime");
        export const TokenMustBeA0 = new MessageKey("EmailTemplateMessage", "TokenMustBeA0");
    }
    
    export const EmailTemplateMessageEntity_Type = new Type<EmailTemplateMessageEntity>("EmailTemplateMessageEntity");
    export interface EmailTemplateMessageEntity extends Entities.EmbeddedEntity {
        template?: EmailTemplateEntity;
        cultureInfo?: Basics.CultureInfoEntity;
        text?: string;
        subject?: string;
    }
    
    export module EmailTemplateOperation {
        export const CreateEmailTemplateFromSystemEmail : Entities.ConstructSymbol_From<EmailTemplateEntity, SystemEmailEntity> = registerSymbol({ key: "EmailTemplateOperation.CreateEmailTemplateFromSystemEmail" });
        export const Create : Entities.ConstructSymbol_Simple<EmailTemplateEntity> = registerSymbol({ key: "EmailTemplateOperation.Create" });
        export const Save : Entities.ExecuteSymbol<EmailTemplateEntity> = registerSymbol({ key: "EmailTemplateOperation.Save" });
        export const Enable : Entities.ExecuteSymbol<EmailTemplateEntity> = registerSymbol({ key: "EmailTemplateOperation.Enable" });
        export const Disable : Entities.ExecuteSymbol<EmailTemplateEntity> = registerSymbol({ key: "EmailTemplateOperation.Disable" });
    }
    
    export const EmailTemplateRecipientEntity_Type = new Type<EmailTemplateRecipientEntity>("EmailTemplateRecipientEntity");
    export interface EmailTemplateRecipientEntity extends EmailTemplateContactEntity {
        kind?: EmailRecipientKind;
    }
    
    export module EmailTemplateViewMessage {
        export const InsertMessageContent = new MessageKey("EmailTemplateViewMessage", "InsertMessageContent");
        export const Insert = new MessageKey("EmailTemplateViewMessage", "Insert");
        export const Language = new MessageKey("EmailTemplateViewMessage", "Language");
    }
    
    export interface IEmailOwnerEntity extends Entities.IEntity {
    }
    
    export const NewsletterDeliveryEntity_Type = new Type<NewsletterDeliveryEntity>("NewsletterDeliveryEntity");
    export interface NewsletterDeliveryEntity extends Entities.Entity, Processes.IProcessLineDataEntity {
        sent?: boolean;
        sendDate?: string;
        recipient?: Entities.Lite<IEmailOwnerEntity>;
        newsletter?: Entities.Lite<NewsletterEntity>;
    }
    
    export const NewsletterEntity_Type = new Type<NewsletterEntity>("NewsletterEntity");
    export interface NewsletterEntity extends Entities.Entity, Processes.IProcessDataEntity {
        name?: string;
        state?: NewsletterState;
        from?: string;
        displayFrom?: string;
        subject?: string;
        text?: string;
        query?: Entities.Basics.QueryEntity;
    }
    
    export module NewsletterOperation {
        export const Save : Entities.ExecuteSymbol<NewsletterEntity> = registerSymbol({ key: "NewsletterOperation.Save" });
        export const Send : Entities.ConstructSymbol_From<Processes.ProcessEntity, NewsletterEntity> = registerSymbol({ key: "NewsletterOperation.Send" });
        export const AddRecipients : Entities.ExecuteSymbol<NewsletterEntity> = registerSymbol({ key: "NewsletterOperation.AddRecipients" });
        export const RemoveRecipients : Entities.ExecuteSymbol<NewsletterEntity> = registerSymbol({ key: "NewsletterOperation.RemoveRecipients" });
        export const Clone : Entities.ConstructSymbol_From<NewsletterEntity, NewsletterEntity> = registerSymbol({ key: "NewsletterOperation.Clone" });
    }
    
    export module NewsletterProcess {
        export const SendNewsletter : Processes.ProcessAlgorithmSymbol = registerSymbol({ key: "NewsletterProcess.SendNewsletter" });
    }
    
    export enum NewsletterState {
        Created,
        Saved,
        Sent,
    }
    export const NewsletterState_Type = new EnumType<NewsletterState>("NewsletterState", NewsletterState);
    
    export module Pop3ConfigurationAction {
        export const ReceiveAllActivePop3Configurations : Scheduler.SimpleTaskSymbol = registerSymbol({ key: "Pop3ConfigurationAction.ReceiveAllActivePop3Configurations" });
    }
    
    export const Pop3ConfigurationEntity_Type = new Type<Pop3ConfigurationEntity>("Pop3ConfigurationEntity");
    export interface Pop3ConfigurationEntity extends Entities.Entity, Scheduler.ITaskEntity {
        active?: boolean;
        port?: number;
        host?: string;
        username?: string;
        password?: string;
        enableSSL?: boolean;
        readTimeout?: number;
        deleteMessagesAfter?: number;
        clientCertificationFiles?: Entities.MList<ClientCertificationFileEntity>;
    }
    
    export module Pop3ConfigurationOperation {
        export const Save : Entities.ExecuteSymbol<Pop3ConfigurationEntity> = registerSymbol({ key: "Pop3ConfigurationOperation.Save" });
        export const ReceiveEmails : Entities.ConstructSymbol_From<Pop3ReceptionEntity, Pop3ConfigurationEntity> = registerSymbol({ key: "Pop3ConfigurationOperation.ReceiveEmails" });
    }
    
    export const Pop3ReceptionEntity_Type = new Type<Pop3ReceptionEntity>("Pop3ReceptionEntity");
    export interface Pop3ReceptionEntity extends Entities.Entity {
        pop3Configuration?: Entities.Lite<Pop3ConfigurationEntity>;
        startDate?: string;
        endDate?: string;
        newEmails?: number;
        exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
    }
    
    export const Pop3ReceptionExceptionEntity_Type = new Type<Pop3ReceptionExceptionEntity>("Pop3ReceptionExceptionEntity");
    export interface Pop3ReceptionExceptionEntity extends Entities.Entity {
        reception?: Entities.Lite<Pop3ReceptionEntity>;
        exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
    }
    
    export const SmtpConfigurationEntity_Type = new Type<SmtpConfigurationEntity>("SmtpConfigurationEntity");
    export interface SmtpConfigurationEntity extends Entities.Entity {
        name?: string;
        deliveryFormat?: External.SmtpDeliveryFormat;
        deliveryMethod?: External.SmtpDeliveryMethod;
        network?: SmtpNetworkDeliveryEntity;
        pickupDirectoryLocation?: string;
        defaultFrom?: EmailAddressEntity;
        additionalRecipients?: Entities.MList<EmailRecipientEntity>;
    }
    
    export module SmtpConfigurationOperation {
        export const Save : Entities.ExecuteSymbol<SmtpConfigurationEntity> = registerSymbol({ key: "SmtpConfigurationOperation.Save" });
    }
    
    export const SmtpNetworkDeliveryEntity_Type = new Type<SmtpNetworkDeliveryEntity>("SmtpNetworkDeliveryEntity");
    export interface SmtpNetworkDeliveryEntity extends Entities.EmbeddedEntity {
        host?: string;
        port?: number;
        username?: string;
        password?: string;
        useDefaultCredentials?: boolean;
        enableSSL?: boolean;
        clientCertificationFiles?: Entities.MList<ClientCertificationFileEntity>;
    }
    
    export const SystemEmailEntity_Type = new Type<SystemEmailEntity>("SystemEmailEntity");
    export interface SystemEmailEntity extends Entities.Entity {
        fullClassName?: string;
    }
    
}

export namespace Map {

    export module MapMessage {
        export const Map = new MessageKey("MapMessage", "Map");
        export const Namespace = new MessageKey("MapMessage", "Namespace");
        export const TableSize = new MessageKey("MapMessage", "TableSize");
        export const Columns = new MessageKey("MapMessage", "Columns");
        export const Rows = new MessageKey("MapMessage", "Rows");
        export const Press0ToExploreEachTable = new MessageKey("MapMessage", "Press0ToExploreEachTable");
        export const Press0ToExploreStatesAndOperations = new MessageKey("MapMessage", "Press0ToExploreStatesAndOperations");
        export const Filter = new MessageKey("MapMessage", "Filter");
        export const Color = new MessageKey("MapMessage", "Color");
        export const State = new MessageKey("MapMessage", "State");
        export const StateColor = new MessageKey("MapMessage", "StateColor");
    }
    
    export module MapPermission {
        export const ViewMap : Authorization.PermissionSymbol = registerSymbol({ key: "MapPermission.ViewMap" });
    }
    
}

export namespace Migrations {

    export const CSharpMigrationEntity_Type = new Type<CSharpMigrationEntity>("CSharpMigrationEntity");
    export interface CSharpMigrationEntity extends Entities.Entity {
        uniqueName?: string;
        executionDate?: string;
    }
    
    export const SqlMigrationEntity_Type = new Type<SqlMigrationEntity>("SqlMigrationEntity");
    export interface SqlMigrationEntity extends Entities.Entity {
        versionNumber?: string;
    }
    
}

export namespace Notes {

    export const NoteEntity_Type = new Type<NoteEntity>("NoteEntity");
    export interface NoteEntity extends Entities.Entity {
        title?: string;
        target?: Entities.Lite<Entities.Entity>;
        creationDate?: string;
        text?: string;
        createdBy?: Entities.Lite<Entities.Basics.IUserEntity>;
        noteType?: NoteTypeEntity;
    }
    
    export module NoteMessage {
        export const NewNote = new MessageKey("NoteMessage", "NewNote");
        export const Note = new MessageKey("NoteMessage", "Note");
        export const _note = new MessageKey("NoteMessage", "_note");
        export const _notes = new MessageKey("NoteMessage", "_notes");
        export const CreateNote = new MessageKey("NoteMessage", "CreateNote");
        export const NoteCreated = new MessageKey("NoteMessage", "NoteCreated");
        export const Notes = new MessageKey("NoteMessage", "Notes");
        export const ViewNotes = new MessageKey("NoteMessage", "ViewNotes");
    }
    
    export module NoteOperation {
        export const CreateNoteFromEntity : Entities.ConstructSymbol_From<NoteEntity, Entities.Entity> = registerSymbol({ key: "NoteOperation.CreateNoteFromEntity" });
        export const Save : Entities.ExecuteSymbol<NoteEntity> = registerSymbol({ key: "NoteOperation.Save" });
    }
    
    export const NoteTypeEntity_Type = new Type<NoteTypeEntity>("NoteTypeEntity");
    export interface NoteTypeEntity extends Entities.Basics.SemiSymbol {
    }
    
    export module NoteTypeOperation {
        export const Save : Entities.ExecuteSymbol<NoteTypeEntity> = registerSymbol({ key: "NoteTypeOperation.Save" });
    }
    
}

export namespace Omnibox {

    export module OmniboxMessage {
        export const No = new MessageKey("OmniboxMessage", "No");
        export const NotFound = new MessageKey("OmniboxMessage", "NotFound");
        export const Omnibox_DatabaseAccess = new MessageKey("OmniboxMessage", "Omnibox_DatabaseAccess");
        export const Omnibox_Disambiguate = new MessageKey("OmniboxMessage", "Omnibox_Disambiguate");
        export const Omnibox_Field = new MessageKey("OmniboxMessage", "Omnibox_Field");
        export const Omnibox_Help = new MessageKey("OmniboxMessage", "Omnibox_Help");
        export const Omnibox_MatchingOptions = new MessageKey("OmniboxMessage", "Omnibox_MatchingOptions");
        export const Omnibox_Query = new MessageKey("OmniboxMessage", "Omnibox_Query");
        export const Omnibox_Type = new MessageKey("OmniboxMessage", "Omnibox_Type");
        export const Omnibox_UserChart = new MessageKey("OmniboxMessage", "Omnibox_UserChart");
        export const Omnibox_UserQuery = new MessageKey("OmniboxMessage", "Omnibox_UserQuery");
        export const Omnibox_Dashboard = new MessageKey("OmniboxMessage", "Omnibox_Dashboard");
        export const Omnibox_Value = new MessageKey("OmniboxMessage", "Omnibox_Value");
        export const Unknown = new MessageKey("OmniboxMessage", "Unknown");
        export const Yes = new MessageKey("OmniboxMessage", "Yes");
        export const ComplementWordsRegex = new MessageKey("OmniboxMessage", "ComplementWordsRegex");
        export const Search = new MessageKey("OmniboxMessage", "Search");
    }
    
}

export namespace Processes {

    export interface IProcessDataEntity extends Entities.IEntity {
    }
    
    export interface IProcessLineDataEntity extends Entities.IEntity {
    }
    
    export const PackageEntity_Type = new Type<PackageEntity>("PackageEntity");
    export interface PackageEntity extends Entities.Entity, IProcessDataEntity {
        name?: string;
        operationArguments?: string;
    }
    
    export const PackageLineEntity_Type = new Type<PackageLineEntity>("PackageLineEntity");
    export interface PackageLineEntity extends Entities.Entity, IProcessLineDataEntity {
        package?: Entities.Lite<PackageEntity>;
        target?: Entities.Entity;
        result?: Entities.Lite<Entities.Entity>;
        finishTime?: string;
    }
    
    export const PackageOperationEntity_Type = new Type<PackageOperationEntity>("PackageOperationEntity");
    export interface PackageOperationEntity extends PackageEntity {
        operation?: Entities.OperationSymbol;
    }
    
    export module PackageOperationProcess {
        export const PackageOperation : ProcessAlgorithmSymbol = registerSymbol({ key: "PackageOperationProcess.PackageOperation" });
    }
    
    export const ProcessAlgorithmSymbol_Type = new Type<ProcessAlgorithmSymbol>("ProcessAlgorithmSymbol");
    export interface ProcessAlgorithmSymbol extends Entities.Symbol {
    }
    
    export const ProcessEntity_Type = new Type<ProcessEntity>("ProcessEntity");
    export interface ProcessEntity extends Entities.Entity {
        algorithm?: ProcessAlgorithmSymbol;
        data?: IProcessDataEntity;
        machineName?: string;
        applicationName?: string;
        user?: Entities.Lite<Entities.Basics.IUserEntity>;
        state?: ProcessState;
        creationDate?: string;
        plannedDate?: string;
        cancelationDate?: string;
        queuedDate?: string;
        executionStart?: string;
        executionEnd?: string;
        suspendDate?: string;
        exceptionDate?: string;
        exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
        progress?: number;
    }
    
    export const ProcessExceptionLineEntity_Type = new Type<ProcessExceptionLineEntity>("ProcessExceptionLineEntity");
    export interface ProcessExceptionLineEntity extends Entities.Entity {
        line?: Entities.Lite<IProcessLineDataEntity>;
        process?: Entities.Lite<ProcessEntity>;
        exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
    }
    
    export module ProcessMessage {
        export const Process0IsNotRunningAnymore = new MessageKey("ProcessMessage", "Process0IsNotRunningAnymore");
        export const ProcessStartIsGreaterThanProcessEnd = new MessageKey("ProcessMessage", "ProcessStartIsGreaterThanProcessEnd");
        export const ProcessStartIsNullButProcessEndIsNot = new MessageKey("ProcessMessage", "ProcessStartIsNullButProcessEndIsNot");
        export const Lines = new MessageKey("ProcessMessage", "Lines");
        export const LastProcess = new MessageKey("ProcessMessage", "LastProcess");
        export const ExceptionLines = new MessageKey("ProcessMessage", "ExceptionLines");
    }
    
    export module ProcessOperation {
        export const Plan : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol({ key: "ProcessOperation.Plan" });
        export const Save : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol({ key: "ProcessOperation.Save" });
        export const Cancel : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol({ key: "ProcessOperation.Cancel" });
        export const Execute : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol({ key: "ProcessOperation.Execute" });
        export const Suspend : Entities.ExecuteSymbol<ProcessEntity> = registerSymbol({ key: "ProcessOperation.Suspend" });
        export const Retry : Entities.ConstructSymbol_From<ProcessEntity, ProcessEntity> = registerSymbol({ key: "ProcessOperation.Retry" });
    }
    
    export module ProcessPermission {
        export const ViewProcessPanel : Authorization.PermissionSymbol = registerSymbol({ key: "ProcessPermission.ViewProcessPanel" });
    }
    
    export enum ProcessState {
        Created,
        Planned,
        Canceled,
        Queued,
        Executing,
        Suspending,
        Suspended,
        Finished,
        Error,
    }
    export const ProcessState_Type = new EnumType<ProcessState>("ProcessState", ProcessState);
    
}

export namespace Profiler {

    export module ProfilerPermission {
        export const ViewTimeTracker : Authorization.PermissionSymbol = registerSymbol({ key: "ProfilerPermission.ViewTimeTracker" });
        export const ViewHeavyProfiler : Authorization.PermissionSymbol = registerSymbol({ key: "ProfilerPermission.ViewHeavyProfiler" });
        export const OverrideSessionTimeout : Authorization.PermissionSymbol = registerSymbol({ key: "ProfilerPermission.OverrideSessionTimeout" });
    }
    
}

export namespace Scheduler {

    export const ApplicationEventLogEntity_Type = new Type<ApplicationEventLogEntity>("ApplicationEventLogEntity");
    export interface ApplicationEventLogEntity extends Entities.Entity {
        machineName?: string;
        date?: string;
        globalEvent?: TypeEvent;
    }
    
    export const HolidayCalendarEntity_Type = new Type<HolidayCalendarEntity>("HolidayCalendarEntity");
    export interface HolidayCalendarEntity extends Entities.Entity {
        name?: string;
        holidays?: Entities.MList<HolidayEntity>;
    }
    
    export module HolidayCalendarOperation {
        export const Save : Entities.ExecuteSymbol<HolidayCalendarEntity> = registerSymbol({ key: "HolidayCalendarOperation.Save" });
        export const Delete : Entities.DeleteSymbol<HolidayCalendarEntity> = registerSymbol({ key: "HolidayCalendarOperation.Delete" });
    }
    
    export const HolidayEntity_Type = new Type<HolidayEntity>("HolidayEntity");
    export interface HolidayEntity extends Entities.EmbeddedEntity {
        date?: string;
        name?: string;
    }
    
    export interface IScheduleRuleEntity extends Entities.IEntity {
    }
    
    export interface ITaskEntity extends Entities.IEntity {
    }
    
    export const ScheduledTaskEntity_Type = new Type<ScheduledTaskEntity>("ScheduledTaskEntity");
    export interface ScheduledTaskEntity extends Entities.Entity {
        rule?: IScheduleRuleEntity;
        task?: ITaskEntity;
        suspended?: boolean;
        machineName?: string;
        user?: Entities.Lite<Entities.Basics.IUserEntity>;
        applicationName?: string;
    }
    
    export const ScheduledTaskLogEntity_Type = new Type<ScheduledTaskLogEntity>("ScheduledTaskLogEntity");
    export interface ScheduledTaskLogEntity extends Entities.Entity {
        scheduledTask?: ScheduledTaskEntity;
        user?: Entities.Lite<Entities.Basics.IUserEntity>;
        task?: ITaskEntity;
        startTime?: string;
        endTime?: string;
        machineName?: string;
        applicationName?: string;
        productEntity?: Entities.Lite<Entities.IEntity>;
        exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
    }
    
    export module ScheduledTaskOperation {
        export const Save : Entities.ExecuteSymbol<ScheduledTaskEntity> = registerSymbol({ key: "ScheduledTaskOperation.Save" });
        export const Delete : Entities.DeleteSymbol<ScheduledTaskEntity> = registerSymbol({ key: "ScheduledTaskOperation.Delete" });
    }
    
    export module SchedulerMessage {
        export const _0IsNotMultiple1 = new MessageKey("SchedulerMessage", "_0IsNotMultiple1");
        export const Each0Hours = new MessageKey("SchedulerMessage", "Each0Hours");
        export const Each0Minutes = new MessageKey("SchedulerMessage", "Each0Minutes");
        export const ScheduleRuleDailyEntity = new MessageKey("SchedulerMessage", "ScheduleRuleDailyEntity");
        export const ScheduleRuleDailyDN_Everydayat = new MessageKey("SchedulerMessage", "ScheduleRuleDailyDN_Everydayat");
        export const ScheduleRuleDayDN_StartingOn = new MessageKey("SchedulerMessage", "ScheduleRuleDayDN_StartingOn");
        export const ScheduleRuleHourlyEntity = new MessageKey("SchedulerMessage", "ScheduleRuleHourlyEntity");
        export const ScheduleRuleMinutelyEntity = new MessageKey("SchedulerMessage", "ScheduleRuleMinutelyEntity");
        export const ScheduleRuleWeekDaysEntity = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysEntity");
        export const ScheduleRuleWeekDaysDN_AndHoliday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_AndHoliday");
        export const ScheduleRuleWeekDaysDN_At = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_At");
        export const ScheduleRuleWeekDaysDN_ButHoliday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_ButHoliday");
        export const ScheduleRuleWeekDaysDN_Calendar = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Calendar");
        export const ScheduleRuleWeekDaysDN_F = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_F");
        export const ScheduleRuleWeekDaysDN_Friday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Friday");
        export const ScheduleRuleWeekDaysDN_Holiday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Holiday");
        export const ScheduleRuleWeekDaysDN_M = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_M");
        export const ScheduleRuleWeekDaysDN_Monday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Monday");
        export const ScheduleRuleWeekDaysDN_S = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_S");
        export const ScheduleRuleWeekDaysDN_Sa = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Sa");
        export const ScheduleRuleWeekDaysDN_Saturday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Saturday");
        export const ScheduleRuleWeekDaysDN_Sunday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Sunday");
        export const ScheduleRuleWeekDaysDN_T = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_T");
        export const ScheduleRuleWeekDaysDN_Th = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Th");
        export const ScheduleRuleWeekDaysDN_Thursday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Thursday");
        export const ScheduleRuleWeekDaysDN_Tuesday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Tuesday");
        export const ScheduleRuleWeekDaysDN_W = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_W");
        export const ScheduleRuleWeekDaysDN_Wednesday = new MessageKey("SchedulerMessage", "ScheduleRuleWeekDaysDN_Wednesday");
        export const ScheduleRuleWeeklyEntity = new MessageKey("SchedulerMessage", "ScheduleRuleWeeklyEntity");
        export const ScheduleRuleWeeklyDN_DayOfTheWeek = new MessageKey("SchedulerMessage", "ScheduleRuleWeeklyDN_DayOfTheWeek");
    }
    
    export module SchedulerPermission {
        export const ViewSchedulerPanel : Authorization.PermissionSymbol = registerSymbol({ key: "SchedulerPermission.ViewSchedulerPanel" });
    }
    
    export const ScheduleRuleDailyEntity_Type = new Type<ScheduleRuleDailyEntity>("ScheduleRuleDailyEntity");
    export interface ScheduleRuleDailyEntity extends ScheduleRuleDayEntity {
    }
    
    export interface ScheduleRuleDayEntity extends Entities.Entity, IScheduleRuleEntity {
        startingOn?: string;
    }
    
    export const ScheduleRuleHourlyEntity_Type = new Type<ScheduleRuleHourlyEntity>("ScheduleRuleHourlyEntity");
    export interface ScheduleRuleHourlyEntity extends Entities.Entity, IScheduleRuleEntity {
        eachHours?: number;
    }
    
    export const ScheduleRuleMinutelyEntity_Type = new Type<ScheduleRuleMinutelyEntity>("ScheduleRuleMinutelyEntity");
    export interface ScheduleRuleMinutelyEntity extends Entities.Entity, IScheduleRuleEntity {
        eachMinutes?: number;
    }
    
    export const ScheduleRuleWeekDaysEntity_Type = new Type<ScheduleRuleWeekDaysEntity>("ScheduleRuleWeekDaysEntity");
    export interface ScheduleRuleWeekDaysEntity extends ScheduleRuleDayEntity {
        monday?: boolean;
        tuesday?: boolean;
        wednesday?: boolean;
        thursday?: boolean;
        friday?: boolean;
        saturday?: boolean;
        sunday?: boolean;
        calendar?: HolidayCalendarEntity;
        holiday?: boolean;
    }
    
    export const ScheduleRuleWeeklyEntity_Type = new Type<ScheduleRuleWeeklyEntity>("ScheduleRuleWeeklyEntity");
    export interface ScheduleRuleWeeklyEntity extends ScheduleRuleDayEntity {
        dayOfTheWeek?: External.DayOfWeek;
    }
    
    export const SimpleTaskSymbol_Type = new Type<SimpleTaskSymbol>("SimpleTaskSymbol");
    export interface SimpleTaskSymbol extends Entities.Symbol, ITaskEntity {
    }
    
    export module TaskMessage {
        export const Execute = new MessageKey("TaskMessage", "Execute");
        export const Executions = new MessageKey("TaskMessage", "Executions");
        export const LastExecution = new MessageKey("TaskMessage", "LastExecution");
    }
    
    export module TaskOperation {
        export const ExecuteSync : Entities.ConstructSymbol_From<Entities.IEntity, ITaskEntity> = registerSymbol({ key: "TaskOperation.ExecuteSync" });
        export const ExecuteAsync : Entities.ExecuteSymbol<ITaskEntity> = registerSymbol({ key: "TaskOperation.ExecuteAsync" });
    }
    
    export enum TypeEvent {
        Start,
        Stop,
    }
    export const TypeEvent_Type = new EnumType<TypeEvent>("TypeEvent", TypeEvent);
    
}

export namespace SMS {

    export enum MessageLengthExceeded {
        NotAllowed,
        Allowed,
        TextPruning,
    }
    export const MessageLengthExceeded_Type = new EnumType<MessageLengthExceeded>("MessageLengthExceeded", MessageLengthExceeded);
    
    export const MultipleSMSModel_Type = new Type<MultipleSMSModel>("MultipleSMSModel");
    export interface MultipleSMSModel extends Entities.ModelEntity {
        message?: string;
        from?: string;
        certified?: boolean;
    }
    
    export const SMSConfigurationEntity_Type = new Type<SMSConfigurationEntity>("SMSConfigurationEntity");
    export interface SMSConfigurationEntity extends Entities.EmbeddedEntity {
        defaultCulture?: Basics.CultureInfoEntity;
    }
    
    export module SmsMessage {
        export const Insert = new MessageKey("SmsMessage", "Insert");
        export const Message = new MessageKey("SmsMessage", "Message");
        export const RemainingCharacters = new MessageKey("SmsMessage", "RemainingCharacters");
        export const RemoveNonValidCharacters = new MessageKey("SmsMessage", "RemoveNonValidCharacters");
        export const StatusCanNotBeUpdatedForNonSentMessages = new MessageKey("SmsMessage", "StatusCanNotBeUpdatedForNonSentMessages");
        export const TheTemplateMustBeActiveToConstructSMSMessages = new MessageKey("SmsMessage", "TheTemplateMustBeActiveToConstructSMSMessages");
        export const TheTextForTheSMSMessageExceedsTheLengthLimit = new MessageKey("SmsMessage", "TheTextForTheSMSMessageExceedsTheLengthLimit");
        export const Language = new MessageKey("SmsMessage", "Language");
        export const Replacements = new MessageKey("SmsMessage", "Replacements");
    }
    
    export const SMSMessageEntity_Type = new Type<SMSMessageEntity>("SMSMessageEntity");
    export interface SMSMessageEntity extends Entities.Entity, Processes.IProcessLineDataEntity {
        template?: Entities.Lite<SMSTemplateEntity>;
        message?: string;
        editableMessage?: boolean;
        from?: string;
        sendDate?: string;
        state?: SMSMessageState;
        destinationNumber?: string;
        messageID?: string;
        certified?: boolean;
        sendPackage?: Entities.Lite<SMSSendPackageEntity>;
        updatePackage?: Entities.Lite<SMSUpdatePackageEntity>;
        updatePackageProcessed?: boolean;
        referred?: Entities.Lite<Entities.Entity>;
        exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
    }
    
    export module SMSMessageOperation {
        export const Send : Entities.ExecuteSymbol<SMSMessageEntity> = registerSymbol({ key: "SMSMessageOperation.Send" });
        export const UpdateStatus : Entities.ExecuteSymbol<SMSMessageEntity> = registerSymbol({ key: "SMSMessageOperation.UpdateStatus" });
        export const CreateUpdateStatusPackage : Entities.ConstructSymbol_FromMany<Processes.ProcessEntity, SMSMessageEntity> = registerSymbol({ key: "SMSMessageOperation.CreateUpdateStatusPackage" });
        export const CreateSMSFromSMSTemplate : Entities.ConstructSymbol_From<SMSMessageEntity, SMSTemplateEntity> = registerSymbol({ key: "SMSMessageOperation.CreateSMSFromSMSTemplate" });
        export const CreateSMSWithTemplateFromEntity : Entities.ConstructSymbol_From<SMSMessageEntity, Entities.Entity> = registerSymbol({ key: "SMSMessageOperation.CreateSMSWithTemplateFromEntity" });
        export const CreateSMSFromEntity : Entities.ConstructSymbol_From<SMSMessageEntity, Entities.Entity> = registerSymbol({ key: "SMSMessageOperation.CreateSMSFromEntity" });
        export const SendSMSMessages : Entities.ConstructSymbol_FromMany<Processes.ProcessEntity, Entities.Entity> = registerSymbol({ key: "SMSMessageOperation.SendSMSMessages" });
        export const SendSMSMessagesFromTemplate : Entities.ConstructSymbol_FromMany<Processes.ProcessEntity, Entities.Entity> = registerSymbol({ key: "SMSMessageOperation.SendSMSMessagesFromTemplate" });
    }
    
    export module SMSMessageProcess {
        export const Send : Processes.ProcessAlgorithmSymbol = registerSymbol({ key: "SMSMessageProcess.Send" });
        export const UpdateStatus : Processes.ProcessAlgorithmSymbol = registerSymbol({ key: "SMSMessageProcess.UpdateStatus" });
    }
    
    export enum SMSMessageState {
        Created,
        Sent,
        Delivered,
        Failed,
    }
    export const SMSMessageState_Type = new EnumType<SMSMessageState>("SMSMessageState", SMSMessageState);
    
    export interface SMSPackageEntity extends Entities.Entity, Processes.IProcessDataEntity {
        name?: string;
    }
    
    export const SMSSendPackageEntity_Type = new Type<SMSSendPackageEntity>("SMSSendPackageEntity");
    export interface SMSSendPackageEntity extends SMSPackageEntity {
    }
    
    export const SMSTemplateEntity_Type = new Type<SMSTemplateEntity>("SMSTemplateEntity");
    export interface SMSTemplateEntity extends Entities.Entity {
        name?: string;
        certified?: boolean;
        editableMessage?: boolean;
        associatedType?: Entities.Basics.TypeEntity;
        messages?: Entities.MList<SMSTemplateMessageEntity>;
        from?: string;
        messageLengthExceeded?: MessageLengthExceeded;
        removeNoSMSCharacters?: boolean;
        active?: boolean;
        startDate?: string;
        endDate?: string;
    }
    
    export module SMSTemplateMessage {
        export const EndDateMustBeHigherThanStartDate = new MessageKey("SMSTemplateMessage", "EndDateMustBeHigherThanStartDate");
        export const ThereAreNoMessagesForTheTemplate = new MessageKey("SMSTemplateMessage", "ThereAreNoMessagesForTheTemplate");
        export const ThereMustBeAMessageFor0 = new MessageKey("SMSTemplateMessage", "ThereMustBeAMessageFor0");
        export const TheresMoreThanOneMessageForTheSameLanguage = new MessageKey("SMSTemplateMessage", "TheresMoreThanOneMessageForTheSameLanguage");
        export const NewCulture = new MessageKey("SMSTemplateMessage", "NewCulture");
    }
    
    export const SMSTemplateMessageEntity_Type = new Type<SMSTemplateMessageEntity>("SMSTemplateMessageEntity");
    export interface SMSTemplateMessageEntity extends Entities.EmbeddedEntity {
        template?: SMSTemplateEntity;
        cultureInfo?: Basics.CultureInfoEntity;
        message?: string;
    }
    
    export module SMSTemplateOperation {
        export const Create : Entities.ConstructSymbol_Simple<SMSTemplateEntity> = registerSymbol({ key: "SMSTemplateOperation.Create" });
        export const Save : Entities.ExecuteSymbol<SMSTemplateEntity> = registerSymbol({ key: "SMSTemplateOperation.Save" });
    }
    
    export const SMSUpdatePackageEntity_Type = new Type<SMSUpdatePackageEntity>("SMSUpdatePackageEntity");
    export interface SMSUpdatePackageEntity extends SMSPackageEntity {
    }
    
}

export namespace Templating {

    export module TemplateTokenMessage {
        export const NoColumnSelected = new MessageKey("TemplateTokenMessage", "NoColumnSelected");
        export const YouCannotAddIfBlocksOnCollectionFields = new MessageKey("TemplateTokenMessage", "YouCannotAddIfBlocksOnCollectionFields");
        export const YouHaveToAddTheElementTokenToUseForeachOnCollectionFields = new MessageKey("TemplateTokenMessage", "YouHaveToAddTheElementTokenToUseForeachOnCollectionFields");
        export const YouCanOnlyAddForeachBlocksWithCollectionFields = new MessageKey("TemplateTokenMessage", "YouCanOnlyAddForeachBlocksWithCollectionFields");
        export const YouCannotAddBlocksWithAllOrAny = new MessageKey("TemplateTokenMessage", "YouCannotAddBlocksWithAllOrAny");
    }
    
}

export namespace Translation {

    export enum TranslatedCultureAction {
        Translate,
        Read,
    }
    export const TranslatedCultureAction_Type = new EnumType<TranslatedCultureAction>("TranslatedCultureAction", TranslatedCultureAction);
    
    export const TranslatedInstanceEntity_Type = new Type<TranslatedInstanceEntity>("TranslatedInstanceEntity");
    export interface TranslatedInstanceEntity extends Entities.Entity {
        culture?: Basics.CultureInfoEntity;
        instance?: Entities.Lite<Entities.Entity>;
        propertyRoute?: Entities.Basics.PropertyRouteEntity;
        rowId?: string;
        translatedText?: string;
        originalText?: string;
    }
    
    export module TranslationJavascriptMessage {
        export const WrongTranslationToSubstitute = new MessageKey("TranslationJavascriptMessage", "WrongTranslationToSubstitute");
        export const RightTranslation = new MessageKey("TranslationJavascriptMessage", "RightTranslation");
        export const RememberChange = new MessageKey("TranslationJavascriptMessage", "RememberChange");
    }
    
    export module TranslationMessage {
        export const RepeatedCultures0 = new MessageKey("TranslationMessage", "RepeatedCultures0");
        export const CodeTranslations = new MessageKey("TranslationMessage", "CodeTranslations");
        export const InstanceTranslations = new MessageKey("TranslationMessage", "InstanceTranslations");
        export const Synchronize0In1 = new MessageKey("TranslationMessage", "Synchronize0In1");
        export const View0In1 = new MessageKey("TranslationMessage", "View0In1");
        export const AllLanguages = new MessageKey("TranslationMessage", "AllLanguages");
        export const _0AlreadySynchronized = new MessageKey("TranslationMessage", "_0AlreadySynchronized");
        export const NothingToTranslate = new MessageKey("TranslationMessage", "NothingToTranslate");
        export const All = new MessageKey("TranslationMessage", "All");
        export const NothingToTranslateIn0 = new MessageKey("TranslationMessage", "NothingToTranslateIn0");
        export const Sync = new MessageKey("TranslationMessage", "Sync");
        export const View = new MessageKey("TranslationMessage", "View");
        export const None = new MessageKey("TranslationMessage", "None");
        export const Edit = new MessageKey("TranslationMessage", "Edit");
        export const Member = new MessageKey("TranslationMessage", "Member");
        export const Type = new MessageKey("TranslationMessage", "Type");
        export const Instance = new MessageKey("TranslationMessage", "Instance");
        export const Property = new MessageKey("TranslationMessage", "Property");
        export const Save = new MessageKey("TranslationMessage", "Save");
        export const Search = new MessageKey("TranslationMessage", "Search");
        export const PressSearchForResults = new MessageKey("TranslationMessage", "PressSearchForResults");
        export const NoResultsFound = new MessageKey("TranslationMessage", "NoResultsFound");
    }
    
    export module TranslationPermission {
        export const TranslateCode : Authorization.PermissionSymbol = registerSymbol({ key: "TranslationPermission.TranslateCode" });
        export const TranslateInstances : Authorization.PermissionSymbol = registerSymbol({ key: "TranslationPermission.TranslateInstances" });
    }
    
    export const TranslationReplacementEntity_Type = new Type<TranslationReplacementEntity>("TranslationReplacementEntity");
    export interface TranslationReplacementEntity extends Entities.Entity {
        cultureInfo?: Basics.CultureInfoEntity;
        wrongTranslation?: string;
        rightTranslation?: string;
    }
    
    export module TranslationReplacementOperation {
        export const Save : Entities.ExecuteSymbol<TranslationReplacementEntity> = registerSymbol({ key: "TranslationReplacementOperation.Save" });
        export const Delete : Entities.DeleteSymbol<TranslationReplacementEntity> = registerSymbol({ key: "TranslationReplacementOperation.Delete" });
    }
    
    export const TranslatorUserCultureEntity_Type = new Type<TranslatorUserCultureEntity>("TranslatorUserCultureEntity");
    export interface TranslatorUserCultureEntity extends Entities.EmbeddedEntity {
        culture?: Basics.CultureInfoEntity;
        action?: TranslatedCultureAction;
    }
    
    export const TranslatorUserEntity_Type = new Type<TranslatorUserEntity>("TranslatorUserEntity");
    export interface TranslatorUserEntity extends Entities.Entity {
        user?: Entities.Lite<Entities.Basics.IUserEntity>;
        cultures?: Entities.MList<TranslatorUserCultureEntity>;
    }
    
    export module TranslatorUserOperation {
        export const Save : Entities.ExecuteSymbol<TranslatorUserEntity> = registerSymbol({ key: "TranslatorUserOperation.Save" });
        export const Delete : Entities.DeleteSymbol<TranslatorUserEntity> = registerSymbol({ key: "TranslatorUserOperation.Delete" });
    }
    
}

export namespace UserAssets {

    export enum EntityAction {
        Identical,
        Different,
        New,
    }
    export const EntityAction_Type = new EnumType<EntityAction>("EntityAction", EntityAction);
    
    export interface IUserAssetEntity extends Entities.IEntity {
        guid?: string;
    }
    
    export const QueryTokenEntity_Type = new Type<QueryTokenEntity>("QueryTokenEntity");
    export interface QueryTokenEntity extends Entities.EmbeddedEntity {
        tokenString?: string;
    }
    
    export module UserAssetMessage {
        export const ExportToXml = new MessageKey("UserAssetMessage", "ExportToXml");
        export const ImportUserAssets = new MessageKey("UserAssetMessage", "ImportUserAssets");
        export const ImportPreview = new MessageKey("UserAssetMessage", "ImportPreview");
        export const SelectTheEntitiesToOverride = new MessageKey("UserAssetMessage", "SelectTheEntitiesToOverride");
        export const SucessfullyImported = new MessageKey("UserAssetMessage", "SucessfullyImported");
    }
    
    export module UserAssetPermission {
        export const UserAssetsToXML : Authorization.PermissionSymbol = registerSymbol({ key: "UserAssetPermission.UserAssetsToXML" });
    }
    
    export const UserAssetPreviewLine_Type = new Type<UserAssetPreviewLine>("UserAssetPreviewLine");
    export interface UserAssetPreviewLine extends Entities.EmbeddedEntity {
        type?: Entities.Basics.TypeEntity;
        text?: string;
        action?: EntityAction;
        overrideEntity?: boolean;
        guid?: string;
    }
    
    export const UserAssetPreviewModel_Type = new Type<UserAssetPreviewModel>("UserAssetPreviewModel");
    export interface UserAssetPreviewModel extends Entities.ModelEntity {
        lines?: Entities.MList<UserAssetPreviewLine>;
    }
    
}

export namespace UserQueries {

    export const QueryColumnEntity_Type = new Type<QueryColumnEntity>("QueryColumnEntity");
    export interface QueryColumnEntity extends Entities.EmbeddedEntity {
        token?: UserAssets.QueryTokenEntity;
        displayName?: string;
    }
    
    export const QueryFilterEntity_Type = new Type<QueryFilterEntity>("QueryFilterEntity");
    export interface QueryFilterEntity extends Entities.EmbeddedEntity {
        token?: UserAssets.QueryTokenEntity;
        operation?: Entities.DynamicQuery.FilterOperation;
        valueString?: string;
    }
    
    export const QueryOrderEntity_Type = new Type<QueryOrderEntity>("QueryOrderEntity");
    export interface QueryOrderEntity extends Entities.EmbeddedEntity {
        token?: UserAssets.QueryTokenEntity;
        orderType?: Entities.DynamicQuery.OrderType;
    }
    
    export const UserQueryEntity_Type = new Type<UserQueryEntity>("UserQueryEntity");
    export interface UserQueryEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
        query?: Entities.Basics.QueryEntity;
        entityType?: Entities.Lite<Entities.Basics.TypeEntity>;
        owner?: Entities.Lite<Entities.Entity>;
        displayName?: string;
        withoutFilters?: boolean;
        filters?: Entities.MList<QueryFilterEntity>;
        orders?: Entities.MList<QueryOrderEntity>;
        columnsMode?: Entities.DynamicQuery.ColumnOptionsMode;
        columns?: Entities.MList<QueryColumnEntity>;
        paginationMode?: Entities.DynamicQuery.PaginationMode;
        elementsPerPage?: number;
        guid?: string;
        shouldHaveElements?: boolean;
    }
    
    export module UserQueryMessage {
        export const AreYouSureToRemove0 = new MessageKey("UserQueryMessage", "AreYouSureToRemove0");
        export const Edit = new MessageKey("UserQueryMessage", "Edit");
        export const MyQueries = new MessageKey("UserQueryMessage", "MyQueries");
        export const RemoveUserQuery = new MessageKey("UserQueryMessage", "RemoveUserQuery");
        export const _0ShouldBeEmptyIf1IsSet = new MessageKey("UserQueryMessage", "_0ShouldBeEmptyIf1IsSet");
        export const _0ShouldBeNullIf1Is2 = new MessageKey("UserQueryMessage", "_0ShouldBeNullIf1Is2");
        export const _0ShouldBeSetIf1Is2 = new MessageKey("UserQueryMessage", "_0ShouldBeSetIf1Is2");
        export const UserQueries_CreateNew = new MessageKey("UserQueryMessage", "UserQueries_CreateNew");
        export const UserQueries_Edit = new MessageKey("UserQueryMessage", "UserQueries_Edit");
        export const UserQueries_UserQueries = new MessageKey("UserQueryMessage", "UserQueries_UserQueries");
        export const TheFilterOperation0isNotCompatibleWith1 = new MessageKey("UserQueryMessage", "TheFilterOperation0isNotCompatibleWith1");
        export const _0IsNotFilterable = new MessageKey("UserQueryMessage", "_0IsNotFilterable");
        export const Use0ToFilterCurrentEntity = new MessageKey("UserQueryMessage", "Use0ToFilterCurrentEntity");
        export const Preview = new MessageKey("UserQueryMessage", "Preview");
    }
    
    export module UserQueryOperation {
        export const Save : Entities.ExecuteSymbol<UserQueryEntity> = registerSymbol({ key: "UserQueryOperation.Save" });
        export const Delete : Entities.DeleteSymbol<UserQueryEntity> = registerSymbol({ key: "UserQueryOperation.Delete" });
    }
    
    export module UserQueryPermission {
        export const ViewUserQuery : Authorization.PermissionSymbol = registerSymbol({ key: "UserQueryPermission.ViewUserQuery" });
    }
    
}

export namespace ViewLog {

    export const ViewLogEntity_Type = new Type<ViewLogEntity>("ViewLogEntity");
    export interface ViewLogEntity extends Entities.Entity {
        target?: Entities.Lite<Entities.Entity>;
        user?: Entities.Lite<Entities.Basics.IUserEntity>;
        viewAction?: string;
        startDate?: string;
        endDate?: string;
        data?: string;
    }
    
}

export namespace Word {

    export const SystemWordTemplateEntity_Type = new Type<SystemWordTemplateEntity>("SystemWordTemplateEntity");
    export interface SystemWordTemplateEntity extends Entities.Entity {
        fullClassName?: string;
    }
    
    export const WordConverterSymbol_Type = new Type<WordConverterSymbol>("WordConverterSymbol");
    export interface WordConverterSymbol extends Entities.Symbol {
    }
    
    export const WordTemplateEntity_Type = new Type<WordTemplateEntity>("WordTemplateEntity");
    export interface WordTemplateEntity extends Entities.Entity {
        name?: string;
        query?: Entities.Basics.QueryEntity;
        systemWordTemplate?: SystemWordTemplateEntity;
        culture?: Basics.CultureInfoEntity;
        active?: boolean;
        startDate?: string;
        endDate?: string;
        disableAuthorization?: boolean;
        template?: Entities.Lite<Files.FileEntity>;
        fileName?: string;
        wordTransformer?: WordTransformerSymbol;
        wordConverter?: WordConverterSymbol;
    }
    
    export module WordTemplateMessage {
        export const ModelShouldBeSetToUseModel0 = new MessageKey("WordTemplateMessage", "ModelShouldBeSetToUseModel0");
        export const Type0DoesNotHaveAPropertyWithName1 = new MessageKey("WordTemplateMessage", "Type0DoesNotHaveAPropertyWithName1");
        export const ChooseAReportTemplate = new MessageKey("WordTemplateMessage", "ChooseAReportTemplate");
    }
    
    export module WordTemplateOperation {
        export const Save : Entities.ExecuteSymbol<WordTemplateEntity> = registerSymbol({ key: "WordTemplateOperation.Save" });
        export const CreateWordReport : Entities.ExecuteSymbol<WordTemplateEntity> = registerSymbol({ key: "WordTemplateOperation.CreateWordReport" });
        export const CreateWordTemplateFromSystemWordTemplate : Entities.ConstructSymbol_From<WordTemplateEntity, SystemWordTemplateEntity> = registerSymbol({ key: "WordTemplateOperation.CreateWordTemplateFromSystemWordTemplate" });
    }
    
    export module WordTemplatePermission {
        export const GenerateReport : Authorization.PermissionSymbol = registerSymbol({ key: "WordTemplatePermission.GenerateReport" });
    }
    
    export const WordTransformerSymbol_Type = new Type<WordTransformerSymbol>("WordTransformerSymbol");
    export interface WordTransformerSymbol extends Entities.Symbol {
    }
    
}

