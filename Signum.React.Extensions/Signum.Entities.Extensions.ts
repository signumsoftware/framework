//Auto-generated from Signum.Entities.Extensions.csproj. Do not modify!

import * as Entities from 'Framework/Signum.React/Scripts/Signum.Entities'

export namespace Alerts {

    export enum AlertCurrentState {
        Attended,
        Alerted,
        Future,
    }
    
    export const AlertEntity: Entities.Type<AlertEntity> = "AlertEntity";
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
        export const Alert = "AlertMessage.Alert"
        export const NewAlert = "AlertMessage.NewAlert"
        export const Alerts = "AlertMessage.Alerts"
        export const Alerts_Attended = "AlertMessage.Alerts_Attended"
        export const Alerts_Future = "AlertMessage.Alerts_Future"
        export const Alerts_NotAttended = "AlertMessage.Alerts_NotAttended"
        export const CheckedAlerts = "AlertMessage.CheckedAlerts"
        export const CreateAlert = "AlertMessage.CreateAlert"
        export const FutureAlerts = "AlertMessage.FutureAlerts"
        export const WarnedAlerts = "AlertMessage.WarnedAlerts"
    }
    
    export module AlertOperation {
        export const CreateAlertFromEntity : Entities.ConstructSymbol_From<AlertEntity, Entities.Entity> = { key: "AlertOperation.CreateAlertFromEntity" };
        export const SaveNew : Entities.ExecuteSymbol<AlertEntity> = { key: "AlertOperation.SaveNew" };
        export const Save : Entities.ExecuteSymbol<AlertEntity> = { key: "AlertOperation.Save" };
        export const Attend : Entities.ExecuteSymbol<AlertEntity> = { key: "AlertOperation.Attend" };
        export const Unattend : Entities.ExecuteSymbol<AlertEntity> = { key: "AlertOperation.Unattend" };
    }
    
    export enum AlertState {
        New,
        Saved,
        Attended,
    }
    
    export const AlertTypeEntity: Entities.Type<AlertTypeEntity> = "AlertTypeEntity";
    export interface AlertTypeEntity extends Entities.Basics.SemiSymbol {
    }
    
    export module AlertTypeOperation {
        export const Save : Entities.ExecuteSymbol<AlertTypeEntity> = { key: "AlertTypeOperation.Save" };
    }
    
}

export namespace Authorization {

    export interface AllowedRule extends Entities.ModelEntity {
        allowedBase?: any;
        allowed?: any;
        overriden?: boolean;
        resource?: any;
    }
    
    export interface AllowedRuleCoerced extends AllowedRule {
        coercedValues?: any;
    }
    
    export module AuthAdminMessage {
        export const _0of1 = "AuthAdminMessage._0of1"
        export const Nothing = "AuthAdminMessage.Nothing"
        export const Everything = "AuthAdminMessage.Everything"
        export const TypeRules = "AuthAdminMessage.TypeRules"
        export const PermissionRules = "AuthAdminMessage.PermissionRules"
        export const Allow = "AuthAdminMessage.Allow"
        export const Deny = "AuthAdminMessage.Deny"
        export const Overriden = "AuthAdminMessage.Overriden"
    }
    
    export module AuthEmailMessage {
        export const YouRecentlyRequestedANewPassword = "AuthEmailMessage.YouRecentlyRequestedANewPassword"
        export const YourUsernameIs = "AuthEmailMessage.YourUsernameIs"
        export const YouCanResetYourPasswordByFollowingTheLinkBelow = "AuthEmailMessage.YouCanResetYourPasswordByFollowingTheLinkBelow"
        export const ResetPasswordRequestSubject = "AuthEmailMessage.ResetPasswordRequestSubject"
    }
    
    export module AuthMessage {
        export const _0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships = "AuthMessage._0CyclesHaveBeenFoundInTheGraphOfRolesDueToTheRelationships"
        export const _0RulesFor1 = "AuthMessage._0RulesFor1"
        export const AuthAdmin_AddCondition = "AuthMessage.AuthAdmin_AddCondition"
        export const AuthAdmin_ChooseACondition = "AuthMessage.AuthAdmin_ChooseACondition"
        export const AuthAdmin_RemoveCondition = "AuthMessage.AuthAdmin_RemoveCondition"
        export const AuthorizationCacheSuccessfullyUpdated = "AuthMessage.AuthorizationCacheSuccessfullyUpdated"
        export const ChangePassword = "AuthMessage.ChangePassword"
        export const ChangePasswordAspx_ActualPassword = "AuthMessage.ChangePasswordAspx_ActualPassword"
        export const ChangePasswordAspx_ChangePassword = "AuthMessage.ChangePasswordAspx_ChangePassword"
        export const ChangePasswordAspx_ConfirmNewPassword = "AuthMessage.ChangePasswordAspx_ConfirmNewPassword"
        export const ChangePasswordAspx_NewPassword = "AuthMessage.ChangePasswordAspx_NewPassword"
        export const ChangePasswordAspx_WriteActualPasswordAndNewOne = "AuthMessage.ChangePasswordAspx_WriteActualPasswordAndNewOne"
        export const ConfirmNewPassword = "AuthMessage.ConfirmNewPassword"
        export const EmailMustHaveAValue = "AuthMessage.EmailMustHaveAValue"
        export const EmailSent = "AuthMessage.EmailSent"
        export const Email = "AuthMessage.Email"
        export const EnterTheNewPassword = "AuthMessage.EnterTheNewPassword"
        export const EntityGroupsAscx_EntityGroup = "AuthMessage.EntityGroupsAscx_EntityGroup"
        export const EntityGroupsAscx_Overriden = "AuthMessage.EntityGroupsAscx_Overriden"
        export const ExpectedUserLogged = "AuthMessage.ExpectedUserLogged"
        export const ExpiredPassword = "AuthMessage.ExpiredPassword"
        export const ExpiredPasswordMessage = "AuthMessage.ExpiredPasswordMessage"
        export const ForgotYourPasswordEnterYourPasswordBelow = "AuthMessage.ForgotYourPasswordEnterYourPasswordBelow"
        export const WeWillSendYouAnEmailWithALinkToResetYourPassword = "AuthMessage.WeWillSendYouAnEmailWithALinkToResetYourPassword"
        export const IHaveForgottenMyPassword = "AuthMessage.IHaveForgottenMyPassword"
        export const IncorrectPassword = "AuthMessage.IncorrectPassword"
        export const IntroduceYourUserNameAndPassword = "AuthMessage.IntroduceYourUserNameAndPassword"
        export const InvalidUsernameOrPassword = "AuthMessage.InvalidUsernameOrPassword"
        export const InvalidUsername = "AuthMessage.InvalidUsername"
        export const InvalidPassword = "AuthMessage.InvalidPassword"
        export const Login_New = "AuthMessage.Login_New"
        export const Login_Password = "AuthMessage.Login_Password"
        export const Login_Repeat = "AuthMessage.Login_Repeat"
        export const Login_UserName = "AuthMessage.Login_UserName"
        export const Login_UserName_Watermark = "AuthMessage.Login_UserName_Watermark"
        export const Login = "AuthMessage.Login"
        export const Logout = "AuthMessage.Logout"
        export const NewPassword = "AuthMessage.NewPassword"
        export const NotAllowedToSaveThis0WhileOffline = "AuthMessage.NotAllowedToSaveThis0WhileOffline"
        export const NotAuthorizedTo0The1WithId2 = "AuthMessage.NotAuthorizedTo0The1WithId2"
        export const NotAuthorizedToRetrieve0 = "AuthMessage.NotAuthorizedToRetrieve0"
        export const NotAuthorizedToSave0 = "AuthMessage.NotAuthorizedToSave0"
        export const NotAuthorizedToChangeProperty0on1 = "AuthMessage.NotAuthorizedToChangeProperty0on1"
        export const NotUserLogged = "AuthMessage.NotUserLogged"
        export const Password = "AuthMessage.Password"
        export const PasswordChanged = "AuthMessage.PasswordChanged"
        export const PasswordDoesNotMatchCurrent = "AuthMessage.PasswordDoesNotMatchCurrent"
        export const PasswordHasBeenChangedSuccessfully = "AuthMessage.PasswordHasBeenChangedSuccessfully"
        export const PasswordMustHaveAValue = "AuthMessage.PasswordMustHaveAValue"
        export const YourPasswordIsNearExpiration = "AuthMessage.YourPasswordIsNearExpiration"
        export const PasswordsAreDifferent = "AuthMessage.PasswordsAreDifferent"
        export const PasswordsDoNotMatch = "AuthMessage.PasswordsDoNotMatch"
        export const Please0IntoYourAccount = "AuthMessage.Please0IntoYourAccount"
        export const PleaseEnterYourChosenNewPassword = "AuthMessage.PleaseEnterYourChosenNewPassword"
        export const Remember = "AuthMessage.Remember"
        export const RememberMe = "AuthMessage.RememberMe"
        export const ResetPassword = "AuthMessage.ResetPassword"
        export const ResetPasswordCode = "AuthMessage.ResetPasswordCode"
        export const ResetPasswordCodeHasBeenSent = "AuthMessage.ResetPasswordCodeHasBeenSent"
        export const ResetPasswordSuccess = "AuthMessage.ResetPasswordSuccess"
        export const Save = "AuthMessage.Save"
        export const TheConfirmationCodeThatYouHaveJustSentIsInvalid = "AuthMessage.TheConfirmationCodeThatYouHaveJustSentIsInvalid"
        export const ThePasswordMustHaveBetween7And15CharactersEachOfThemBeingANumber09OrALetter = "AuthMessage.ThePasswordMustHaveBetween7And15CharactersEachOfThemBeingANumber09OrALetter"
        export const ThereHasBeenAnErrorWithYourRequestToResetYourPasswordPleaseEnterYourLogin = "AuthMessage.ThereHasBeenAnErrorWithYourRequestToResetYourPasswordPleaseEnterYourLogin"
        export const ThereSNotARegisteredUserWithThatEmailAddress = "AuthMessage.ThereSNotARegisteredUserWithThatEmailAddress"
        export const TheSpecifiedPasswordsDontMatch = "AuthMessage.TheSpecifiedPasswordsDontMatch"
        export const TheUserStateMustBeDisabled = "AuthMessage.TheUserStateMustBeDisabled"
        export const Username = "AuthMessage.Username"
        export const Username0IsNotValid = "AuthMessage.Username0IsNotValid"
        export const UserNameMustHaveAValue = "AuthMessage.UserNameMustHaveAValue"
        export const View = "AuthMessage.View"
        export const WeReceivedARequestToCreateAnAccountYouCanCreateItFollowingTheLinkBelow = "AuthMessage.WeReceivedARequestToCreateAnAccountYouCanCreateItFollowingTheLinkBelow"
        export const YouMustRepeatTheNewPassword = "AuthMessage.YouMustRepeatTheNewPassword"
        export const User0IsDisabled = "AuthMessage.User0IsDisabled"
        export const SendEmail = "AuthMessage.SendEmail"
        export const Welcome0 = "AuthMessage.Welcome0"
        export const LoginWithAnotherUser = "AuthMessage.LoginWithAnotherUser"
    }
    
    export enum AuthThumbnail {
        All,
        Mix,
        None,
    }
    
    export interface BaseRulePack extends Entities.ModelEntity {
        role?: Entities.Lite<RoleEntity>;
        strategy?: string;
        type?: Entities.Basics.TypeEntity;
        rules?: Entities.MList<any>;
    }
    
    export module BasicPermission {
        export const AdminRules : PermissionSymbol = { key: "BasicPermission.AdminRules" };
        export const AutomaticUpgradeOfProperties : PermissionSymbol = { key: "BasicPermission.AutomaticUpgradeOfProperties" };
        export const AutomaticUpgradeOfQueries : PermissionSymbol = { key: "BasicPermission.AutomaticUpgradeOfQueries" };
        export const AutomaticUpgradeOfOperations : PermissionSymbol = { key: "BasicPermission.AutomaticUpgradeOfOperations" };
    }
    
    export const LastAuthRulesImportEntity: Entities.Type<LastAuthRulesImportEntity> = "LastAuthRulesImportEntity";
    export interface LastAuthRulesImportEntity extends Entities.Entity {
        date?: string;
    }
    
    export enum MergeStrategy {
        Union,
        Intersection,
    }
    
    export const OperationAllowedRule: Entities.Type<OperationAllowedRule> = "OperationAllowedRule";
    export interface OperationAllowedRule extends AllowedRuleCoerced {
    }
    
    export const OperationRulePack: Entities.Type<OperationRulePack> = "OperationRulePack";
    export interface OperationRulePack extends BaseRulePack {
    }
    
    export const PasswordExpiresIntervalEntity: Entities.Type<PasswordExpiresIntervalEntity> = "PasswordExpiresIntervalEntity";
    export interface PasswordExpiresIntervalEntity extends Entities.Entity {
        days?: number;
        daysWarning?: number;
        enabled?: boolean;
    }
    
    export module PasswordExpiresIntervalOperation {
        export const Save : Entities.ExecuteSymbol<PasswordExpiresIntervalEntity> = { key: "PasswordExpiresIntervalOperation.Save" };
    }
    
    export const PermissionAllowedRule: Entities.Type<PermissionAllowedRule> = "PermissionAllowedRule";
    export interface PermissionAllowedRule extends AllowedRule {
    }
    
    export const PermissionRulePack: Entities.Type<PermissionRulePack> = "PermissionRulePack";
    export interface PermissionRulePack extends BaseRulePack {
    }
    
    export const PermissionSymbol: Entities.Type<PermissionSymbol> = "PermissionSymbol";
    export interface PermissionSymbol extends Entities.Symbol {
    }
    
    export const PropertyAllowedRule: Entities.Type<PropertyAllowedRule> = "PropertyAllowedRule";
    export interface PropertyAllowedRule extends AllowedRuleCoerced {
    }
    
    export const PropertyRulePack: Entities.Type<PropertyRulePack> = "PropertyRulePack";
    export interface PropertyRulePack extends BaseRulePack {
    }
    
    export const QueryAllowedRule: Entities.Type<QueryAllowedRule> = "QueryAllowedRule";
    export interface QueryAllowedRule extends AllowedRuleCoerced {
    }
    
    export const QueryRulePack: Entities.Type<QueryRulePack> = "QueryRulePack";
    export interface QueryRulePack extends BaseRulePack {
    }
    
    export const ResetPasswordRequestEntity: Entities.Type<ResetPasswordRequestEntity> = "ResetPasswordRequestEntity";
    export interface ResetPasswordRequestEntity extends Entities.Entity {
        code?: string;
        user?: UserEntity;
        requestDate?: string;
        lapsed?: boolean;
    }
    
    export const RoleEntity: Entities.Type<RoleEntity> = "RoleEntity";
    export interface RoleEntity extends Entities.Entity {
        name?: string;
        mergeStrategy?: MergeStrategy;
        roles?: Entities.MList<Entities.Lite<RoleEntity>>;
    }
    
    export module RoleOperation {
        export const Save : Entities.ExecuteSymbol<RoleEntity> = { key: "RoleOperation.Save" };
        export const Delete : Entities.DeleteSymbol<RoleEntity> = { key: "RoleOperation.Delete" };
    }
    
    export const RuleEntity: Entities.Type<RuleEntity> = "RuleEntity";
    export interface RuleEntity extends Entities.Entity {
        role?: Entities.Lite<RoleEntity>;
        resource?: any;
        allowed?: any;
    }
    
    export const RuleOperationEntity: Entities.Type<RuleOperationEntity> = "RuleOperationEntity";
    export interface RuleOperationEntity extends RuleEntity {
    }
    
    export const RulePermissionEntity: Entities.Type<RulePermissionEntity> = "RulePermissionEntity";
    export interface RulePermissionEntity extends RuleEntity {
    }
    
    export const RulePropertyEntity: Entities.Type<RulePropertyEntity> = "RulePropertyEntity";
    export interface RulePropertyEntity extends RuleEntity {
    }
    
    export const RuleQueryEntity: Entities.Type<RuleQueryEntity> = "RuleQueryEntity";
    export interface RuleQueryEntity extends RuleEntity {
    }
    
    export const RuleTypeConditionEntity: Entities.Type<RuleTypeConditionEntity> = "RuleTypeConditionEntity";
    export interface RuleTypeConditionEntity extends Entities.EmbeddedEntity {
        condition?: Basics.TypeConditionSymbol;
        allowed?: TypeAllowed;
    }
    
    export const RuleTypeEntity: Entities.Type<RuleTypeEntity> = "RuleTypeEntity";
    export interface RuleTypeEntity extends RuleEntity {
        conditions?: Entities.MList<RuleTypeConditionEntity>;
    }
    
    export const SessionLogEntity: Entities.Type<SessionLogEntity> = "SessionLogEntity";
    export interface SessionLogEntity extends Entities.Entity {
        user?: Entities.Lite<UserEntity>;
        sessionStart?: string;
        sessionEnd?: string;
        sessionTimeOut?: boolean;
        userHostAddress?: string;
        userAgent?: string;
    }
    
    export module SessionLogPermission {
        export const TrackSession : PermissionSymbol = { key: "SessionLogPermission.TrackSession" };
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
    
    export const TypeAllowedAndConditions: Entities.Type<TypeAllowedAndConditions> = "TypeAllowedAndConditions";
    export interface TypeAllowedAndConditions extends Entities.ModelEntity {
        fallback?: TypeAllowed;
        fallbackOrNone?: TypeAllowed;
        conditions?: Array<TypeConditionRule>;
    }
    
    export const TypeAllowedRule: Entities.Type<TypeAllowedRule> = "TypeAllowedRule";
    export interface TypeAllowedRule extends AllowedRule {
        properties?: AuthThumbnail;
        operations?: AuthThumbnail;
        queries?: AuthThumbnail;
        availableConditions?: Array<Basics.TypeConditionSymbol>;
    }
    
    export const TypeConditionRule: Entities.Type<TypeConditionRule> = "TypeConditionRule";
    export interface TypeConditionRule extends Entities.EmbeddedEntity {
        typeCondition?: Basics.TypeConditionSymbol;
        allowed?: TypeAllowed;
    }
    
    export const TypeRulePack: Entities.Type<TypeRulePack> = "TypeRulePack";
    export interface TypeRulePack extends BaseRulePack {
    }
    
    export const UserEntity: Entities.Type<UserEntity> = "UserEntity";
    export interface UserEntity extends Entities.Entity, Mailing.IEmailOwnerEntity, Entities.Basics.IUserEntity {
        userName?: string;
        passwordHash?: any;
        passwordSetDate?: string;
        passwordNeverExpires?: boolean;
        role?: RoleEntity;
        email?: string;
        cultureInfo?: Basics.CultureInfoEntity;
        anulationDate?: string;
        state?: UserState;
    }
    
    export module UserOperation {
        export const Create : Entities.ConstructSymbol_Simple<UserEntity> = { key: "UserOperation.Create" };
        export const SaveNew : Entities.ExecuteSymbol<UserEntity> = { key: "UserOperation.SaveNew" };
        export const Save : Entities.ExecuteSymbol<UserEntity> = { key: "UserOperation.Save" };
        export const Enable : Entities.ExecuteSymbol<UserEntity> = { key: "UserOperation.Enable" };
        export const Disable : Entities.ExecuteSymbol<UserEntity> = { key: "UserOperation.Disable" };
        export const SetPassword : Entities.ExecuteSymbol<UserEntity> = { key: "UserOperation.SetPassword" };
    }
    
    export enum UserState {
        New = -1,
        Saved,
        Disabled,
    }
    
    export const UserTicketEntity: Entities.Type<UserTicketEntity> = "UserTicketEntity";
    export interface UserTicketEntity extends Entities.Entity {
        user?: Entities.Lite<UserEntity>;
        ticket?: string;
        connectionDate?: string;
        device?: string;
    }
    
}

export namespace Basics {

    export const CultureInfoEntity: Entities.Type<CultureInfoEntity> = "CultureInfoEntity";
    export interface CultureInfoEntity extends Entities.Entity {
        name?: string;
        nativeName?: string;
        englishName?: string;
    }
    
    export module CultureInfoOperation {
        export const Save : Entities.ExecuteSymbol<CultureInfoEntity> = { key: "CultureInfoOperation.Save" };
    }
    
    export const DateSpanEntity: Entities.Type<DateSpanEntity> = "DateSpanEntity";
    export interface DateSpanEntity extends Entities.EmbeddedEntity {
        years?: number;
        months?: number;
        days?: number;
    }
    
    export const PropertyRouteEntity: Entities.Type<PropertyRouteEntity> = "PropertyRouteEntity";
    export interface PropertyRouteEntity extends Entities.Entity {
        path?: string;
        rootType?: Entities.Basics.TypeEntity;
    }
    
    export const QueryEntity: Entities.Type<QueryEntity> = "QueryEntity";
    export interface QueryEntity extends Entities.Entity {
        name?: string;
        key?: string;
    }
    
    export const TypeConditionSymbol: Entities.Type<TypeConditionSymbol> = "TypeConditionSymbol";
    export interface TypeConditionSymbol extends Entities.Symbol {
    }
    
}

export namespace Cache {

    export module CachePermission {
        export const ViewCache : Authorization.PermissionSymbol = { key: "CachePermission.ViewCache" };
        export const InvalidateCache : Authorization.PermissionSymbol = { key: "CachePermission.InvalidateCache" };
    }
    
}

export namespace Chart {

    export const ChartColorEntity: Entities.Type<ChartColorEntity> = "ChartColorEntity";
    export interface ChartColorEntity extends Entities.Entity {
        related?: Entities.Lite<Entities.Entity>;
        color?: Entities.Basics.ColorEntity;
    }
    
    export const ChartColumnEntity: Entities.Type<ChartColumnEntity> = "ChartColumnEntity";
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
    
    export module ChartMessage {
        export const _0CanOnlyBeCreatedFromTheChartWindow = "ChartMessage._0CanOnlyBeCreatedFromTheChartWindow"
        export const _0CanOnlyBeCreatedFromTheSearchWindow = "ChartMessage._0CanOnlyBeCreatedFromTheSearchWindow"
        export const Chart = "ChartMessage.Chart"
        export const ChartToken = "ChartMessage.ChartToken"
        export const Chart_ChartSettings = "ChartMessage.Chart_ChartSettings"
        export const Chart_Dimension = "ChartMessage.Chart_Dimension"
        export const Chart_Draw = "ChartMessage.Chart_Draw"
        export const Chart_Group = "ChartMessage.Chart_Group"
        export const Chart_Query0IsNotAllowed = "ChartMessage.Chart_Query0IsNotAllowed"
        export const Chart_ToggleInfo = "ChartMessage.Chart_ToggleInfo"
        export const EditScript = "ChartMessage.EditScript"
        export const ColorsFor0 = "ChartMessage.ColorsFor0"
        export const CreatePalette = "ChartMessage.CreatePalette"
        export const MyCharts = "ChartMessage.MyCharts"
        export const CreateNew = "ChartMessage.CreateNew"
        export const EditUserChart = "ChartMessage.EditUserChart"
        export const ViewPalette = "ChartMessage.ViewPalette"
        export const ChartFor = "ChartMessage.ChartFor"
        export const ChartOf0 = "ChartMessage.ChartOf0"
        export const _0IsKeyBut1IsAnAggregate = "ChartMessage._0IsKeyBut1IsAnAggregate"
        export const _0ShouldBeAnAggregate = "ChartMessage._0ShouldBeAnAggregate"
        export const _0ShouldBeSet = "ChartMessage._0ShouldBeSet"
        export const _0ShouldBeNull = "ChartMessage._0ShouldBeNull"
        export const _0IsNot1 = "ChartMessage._0IsNot1"
        export const _0IsAnAggregateButTheChartIsNotGrouping = "ChartMessage._0IsAnAggregateButTheChartIsNotGrouping"
        export const _0IsNotOptional = "ChartMessage._0IsNotOptional"
        export const SavePalette = "ChartMessage.SavePalette"
        export const NewPalette = "ChartMessage.NewPalette"
        export const Data = "ChartMessage.Data"
        export const ChooseABasePalette = "ChartMessage.ChooseABasePalette"
        export const DeletePalette = "ChartMessage.DeletePalette"
        export const Preview = "ChartMessage.Preview"
    }
    
    export const ChartPaletteModel: Entities.Type<ChartPaletteModel> = "ChartPaletteModel";
    export interface ChartPaletteModel extends Entities.ModelEntity {
        type?: Entities.Basics.TypeEntity;
        colors?: Entities.MList<ChartColorEntity>;
    }
    
    export const ChartParameterEntity: Entities.Type<ChartParameterEntity> = "ChartParameterEntity";
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
    
    export module ChartPermission {
        export const ViewCharting : Authorization.PermissionSymbol = { key: "ChartPermission.ViewCharting" };
    }
    
    export const ChartScriptColumnEntity: Entities.Type<ChartScriptColumnEntity> = "ChartScriptColumnEntity";
    export interface ChartScriptColumnEntity extends Entities.EmbeddedEntity {
        displayName?: string;
        isOptional?: boolean;
        columnType?: ChartColumnType;
        isGroupKey?: boolean;
    }
    
    export const ChartScriptEntity: Entities.Type<ChartScriptEntity> = "ChartScriptEntity";
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
        export const Save : Entities.ExecuteSymbol<ChartScriptEntity> = { key: "ChartScriptOperation.Save" };
        export const Clone : Entities.ConstructSymbol_From<ChartScriptEntity, ChartScriptEntity> = { key: "ChartScriptOperation.Clone" };
        export const Delete : Entities.DeleteSymbol<ChartScriptEntity> = { key: "ChartScriptOperation.Delete" };
    }
    
    export const ChartScriptParameterEntity: Entities.Type<ChartScriptParameterEntity> = "ChartScriptParameterEntity";
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
    
    export const UserChartEntity: Entities.Type<UserChartEntity> = "UserChartEntity";
    export interface UserChartEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
        query?: Basics.QueryEntity;
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
        export const Save : Entities.ExecuteSymbol<UserChartEntity> = { key: "UserChartOperation.Save" };
        export const Delete : Entities.DeleteSymbol<UserChartEntity> = { key: "UserChartOperation.Delete" };
    }
    
}

export namespace Dashboard {

    export const CountSearchControlPartEntity: Entities.Type<CountSearchControlPartEntity> = "CountSearchControlPartEntity";
    export interface CountSearchControlPartEntity extends Entities.Entity, IPartEntity {
        userQueries?: Entities.MList<CountUserQueryElementEntity>;
        requiresTitle?: boolean;
    }
    
    export const CountUserQueryElementEntity: Entities.Type<CountUserQueryElementEntity> = "CountUserQueryElementEntity";
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
    
    export const DashboardEntity: Entities.Type<DashboardEntity> = "DashboardEntity";
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
        export const CreateNewPart = "DashboardMessage.CreateNewPart"
        export const DashboardDN_TitleMustBeSpecifiedFor0 = "DashboardMessage.DashboardDN_TitleMustBeSpecifiedFor0"
        export const CountSearchControlPartEntity = "DashboardMessage.CountSearchControlPartEntity"
        export const CountUserQueryElement = "DashboardMessage.CountUserQueryElement"
        export const Preview = "DashboardMessage.Preview"
        export const _0Is1InstedOf2In3 = "DashboardMessage._0Is1InstedOf2In3"
        export const Part0IsTooLarge = "DashboardMessage.Part0IsTooLarge"
        export const Part0OverlapsWith1 = "DashboardMessage.Part0OverlapsWith1"
    }
    
    export module DashboardOperation {
        export const Create : Entities.ConstructSymbol_Simple<DashboardEntity> = { key: "DashboardOperation.Create" };
        export const Save : Entities.ExecuteSymbol<DashboardEntity> = { key: "DashboardOperation.Save" };
        export const Clone : Entities.ConstructSymbol_From<DashboardEntity, DashboardEntity> = { key: "DashboardOperation.Clone" };
        export const Delete : Entities.DeleteSymbol<DashboardEntity> = { key: "DashboardOperation.Delete" };
    }
    
    export module DashboardPermission {
        export const ViewDashboard : Authorization.PermissionSymbol = { key: "DashboardPermission.ViewDashboard" };
    }
    
    export interface IPartEntity extends Entities.IEntity {
        requiresTitle?: boolean;
    }
    
    export const LinkElementEntity: Entities.Type<LinkElementEntity> = "LinkElementEntity";
    export interface LinkElementEntity extends Entities.EmbeddedEntity {
        label?: string;
        link?: string;
    }
    
    export const LinkListPartEntity: Entities.Type<LinkListPartEntity> = "LinkListPartEntity";
    export interface LinkListPartEntity extends Entities.Entity, IPartEntity {
        links?: Entities.MList<LinkElementEntity>;
        requiresTitle?: boolean;
    }
    
    export const PanelPartEntity: Entities.Type<PanelPartEntity> = "PanelPartEntity";
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
    
    export const UserChartPartEntity: Entities.Type<UserChartPartEntity> = "UserChartPartEntity";
    export interface UserChartPartEntity extends Entities.Entity, IPartEntity {
        userChart?: Chart.UserChartEntity;
        showData?: boolean;
        requiresTitle?: boolean;
    }
    
    export const UserQueryPartEntity: Entities.Type<UserQueryPartEntity> = "UserQueryPartEntity";
    export interface UserQueryPartEntity extends Entities.Entity, IPartEntity {
        userQuery?: UserQueries.UserQueryEntity;
        requiresTitle?: boolean;
    }
    
}

export namespace DiffLog {

    export module DiffLogMessage {
        export const PreviousLog = "DiffLogMessage.PreviousLog"
        export const NextLog = "DiffLogMessage.NextLog"
        export const CurrentEntity = "DiffLogMessage.CurrentEntity"
        export const NavigatesToThePreviousOperationLog = "DiffLogMessage.NavigatesToThePreviousOperationLog"
        export const DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState = "DiffLogMessage.DifferenceBetweenFinalStateOfPreviousLogAndTheInitialState"
        export const StateWhenTheOperationStarted = "DiffLogMessage.StateWhenTheOperationStarted"
        export const DifferenceBetweenInitialStateAndFinalState = "DiffLogMessage.DifferenceBetweenInitialStateAndFinalState"
        export const StateWhenTheOperationFinished = "DiffLogMessage.StateWhenTheOperationFinished"
        export const DifferenceBetweenFinalStateAndTheInitialStateOfNextLog = "DiffLogMessage.DifferenceBetweenFinalStateAndTheInitialStateOfNextLog"
        export const NavigatesToTheNextOperationLog = "DiffLogMessage.NavigatesToTheNextOperationLog"
        export const DifferenceBetweenFinalStateAndTheCurrentStateOfTheEntity = "DiffLogMessage.DifferenceBetweenFinalStateAndTheCurrentStateOfTheEntity"
        export const NavigatesToTheCurrentEntity = "DiffLogMessage.NavigatesToTheCurrentEntity"
    }
    
    export const DiffLogMixin: Entities.Type<DiffLogMixin> = "DiffLogMixin";
    export interface DiffLogMixin extends Entities.MixinEntity {
        initialState?: string;
        finalState?: string;
    }
    
}

export namespace Disconnected {

    export const DisconnectedCreatedMixin: Entities.Type<DisconnectedCreatedMixin> = "DisconnectedCreatedMixin";
    export interface DisconnectedCreatedMixin extends Entities.MixinEntity {
        disconnectedCreated?: boolean;
    }
    
    export const DisconnectedExportEntity: Entities.Type<DisconnectedExportEntity> = "DisconnectedExportEntity";
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
    
    export const DisconnectedExportTableEntity: Entities.Type<DisconnectedExportTableEntity> = "DisconnectedExportTableEntity";
    export interface DisconnectedExportTableEntity extends Entities.EmbeddedEntity {
        type?: Entities.Lite<Entities.Basics.TypeEntity>;
        copyTable?: number;
        errors?: string;
    }
    
    export const DisconnectedImportEntity: Entities.Type<DisconnectedImportEntity> = "DisconnectedImportEntity";
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
    
    export const DisconnectedImportTableEntity: Entities.Type<DisconnectedImportTableEntity> = "DisconnectedImportTableEntity";
    export interface DisconnectedImportTableEntity extends Entities.EmbeddedEntity {
        type?: Entities.Lite<Entities.Basics.TypeEntity>;
        copyTable?: number;
        disableForeignKeys?: boolean;
        insertedRows?: number;
        updatedRows?: number;
        insertedOrUpdated?: number;
    }
    
    export const DisconnectedMachineEntity: Entities.Type<DisconnectedMachineEntity> = "DisconnectedMachineEntity";
    export interface DisconnectedMachineEntity extends Entities.Entity {
        creationDate?: string;
        machineName?: string;
        state?: DisconnectedMachineState;
        seedMin?: number;
        seedMax?: number;
    }
    
    export module DisconnectedMachineOperation {
        export const Save : Entities.ExecuteSymbol<DisconnectedMachineEntity> = { key: "DisconnectedMachineOperation.Save" };
        export const UnsafeUnlock : Entities.ExecuteSymbol<DisconnectedMachineEntity> = { key: "DisconnectedMachineOperation.UnsafeUnlock" };
        export const FixImport : Entities.ConstructSymbol_From<DisconnectedImportEntity, DisconnectedMachineEntity> = { key: "DisconnectedMachineOperation.FixImport" };
    }
    
    export enum DisconnectedMachineState {
        Connected,
        Disconnected,
        Faulted,
        Fixed,
    }
    
    export module DisconnectedMessage {
        export const NotAllowedToSave0WhileOffline = "DisconnectedMessage.NotAllowedToSave0WhileOffline"
        export const The0WithId12IsLockedBy3 = "DisconnectedMessage.The0WithId12IsLockedBy3"
        export const Imports = "DisconnectedMessage.Imports"
        export const Exports = "DisconnectedMessage.Exports"
        export const _0OverlapsWith1 = "DisconnectedMessage._0OverlapsWith1"
    }
    
    export const DisconnectedSubsetMixin: Entities.Type<DisconnectedSubsetMixin> = "DisconnectedSubsetMixin";
    export interface DisconnectedSubsetMixin extends Entities.MixinEntity {
        lastOnlineTicks?: number;
        disconnectedMachine?: Entities.Lite<DisconnectedMachineEntity>;
    }
    
}

export namespace Excel {

    export module ExcelMessage {
        export const Data = "ExcelMessage.Data"
        export const Download = "ExcelMessage.Download"
        export const Excel2007Spreadsheet = "ExcelMessage.Excel2007Spreadsheet"
        export const Administer = "ExcelMessage.Administer"
        export const ExcelReport = "ExcelMessage.ExcelReport"
        export const ExcelTemplateMustHaveExtensionXLSXandCurrentOneHas0 = "ExcelMessage.ExcelTemplateMustHaveExtensionXLSXandCurrentOneHas0"
        export const FindLocationFoExcelReport = "ExcelMessage.FindLocationFoExcelReport"
        export const Reports = "ExcelMessage.Reports"
        export const TheExcelTemplateHasAColumn0NotPresentInTheFindWindow = "ExcelMessage.TheExcelTemplateHasAColumn0NotPresentInTheFindWindow"
        export const ThereAreNoResultsToWrite = "ExcelMessage.ThereAreNoResultsToWrite"
        export const CreateNew = "ExcelMessage.CreateNew"
    }
    
    export const ExcelReportEntity: Entities.Type<ExcelReportEntity> = "ExcelReportEntity";
    export interface ExcelReportEntity extends Entities.Entity {
        query?: Basics.QueryEntity;
        displayName?: string;
        file?: Files.EmbeddedFileEntity;
    }
    
    export module ExcelReportOperation {
        export const Save : Entities.ExecuteSymbol<ExcelReportEntity> = { key: "ExcelReportOperation.Save" };
        export const Delete : Entities.DeleteSymbol<ExcelReportEntity> = { key: "ExcelReportOperation.Delete" };
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
    
    export enum SmtpDeliveryFormat {
        SevenBit,
        International,
    }
    
    export enum SmtpDeliveryMethod {
        Network,
        SpecifiedPickupDirectory,
        PickupDirectoryFromIis,
    }
    
}

export namespace Files {

    export const EmbeddedFileEntity: Entities.Type<EmbeddedFileEntity> = "EmbeddedFileEntity";
    export interface EmbeddedFileEntity extends Entities.EmbeddedEntity {
        fileName?: string;
        binaryFile?: any;
        fullWebPath?: string;
    }
    
    export const EmbeddedFilePathEntity: Entities.Type<EmbeddedFilePathEntity> = "EmbeddedFilePathEntity";
    export interface EmbeddedFilePathEntity extends Entities.EmbeddedEntity {
        fileName?: string;
        binaryFile?: any;
        fileLength?: number;
        fileLengthString?: string;
        sufix?: string;
        calculatedDirectory?: string;
        fileType?: FileTypeSymbol;
        fullPhysicalPath?: string;
        fullWebPath?: string;
    }
    
    export const FileEntity: Entities.Type<FileEntity> = "FileEntity";
    export interface FileEntity extends Entities.ImmutableEntity {
        fileName?: string;
        hash?: string;
        binaryFile?: any;
        fullWebPath?: string;
    }
    
    export module FileMessage {
        export const DownloadFile = "FileMessage.DownloadFile"
        export const ErrorSavingFile = "FileMessage.ErrorSavingFile"
        export const FileTypes = "FileMessage.FileTypes"
        export const Open = "FileMessage.Open"
        export const OpeningHasNotDefaultImplementationFor0 = "FileMessage.OpeningHasNotDefaultImplementationFor0"
        export const WebDownload = "FileMessage.WebDownload"
        export const WebImage = "FileMessage.WebImage"
        export const Remove = "FileMessage.Remove"
        export const SavingHasNotDefaultImplementationFor0 = "FileMessage.SavingHasNotDefaultImplementationFor0"
        export const SelectFile = "FileMessage.SelectFile"
        export const ViewFile = "FileMessage.ViewFile"
        export const ViewingHasNotDefaultImplementationFor0 = "FileMessage.ViewingHasNotDefaultImplementationFor0"
        export const OnlyOneFileIsSupported = "FileMessage.OnlyOneFileIsSupported"
    }
    
    export const FilePathEntity: Entities.Type<FilePathEntity> = "FilePathEntity";
    export interface FilePathEntity extends Entities.Patterns.LockableEntity {
        creationDate?: string;
        fileName?: string;
        binaryFile?: any;
        fileLength?: number;
        fileLengthString?: string;
        sufix?: string;
        calculatedDirectory?: string;
        fileType?: FileTypeSymbol;
        fullPhysicalPath?: string;
        fullWebPath?: string;
    }
    
    export module FilePathOperation {
        export const Save : Entities.ExecuteSymbol<FilePathEntity> = { key: "FilePathOperation.Save" };
    }
    
    export const FileTypeSymbol: Entities.Type<FileTypeSymbol> = "FileTypeSymbol";
    export interface FileTypeSymbol extends Entities.Symbol {
    }
    
}

export namespace Help {

    export const AppendixHelpEntity: Entities.Type<AppendixHelpEntity> = "AppendixHelpEntity";
    export interface AppendixHelpEntity extends Entities.Entity {
        uniqueName?: string;
        culture?: Basics.CultureInfoEntity;
        title?: string;
        description?: string;
    }
    
    export module AppendixHelpOperation {
        export const Save : Entities.ExecuteSymbol<AppendixHelpEntity> = { key: "AppendixHelpOperation.Save" };
    }
    
    export const EntityHelpEntity: Entities.Type<EntityHelpEntity> = "EntityHelpEntity";
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
        export const Save : Entities.ExecuteSymbol<EntityHelpEntity> = { key: "EntityHelpOperation.Save" };
    }
    
    export module HelpKindMessage {
        export const HisMainFunctionIsTo0 = "HelpKindMessage.HisMainFunctionIsTo0"
        export const RelateOtherEntities = "HelpKindMessage.RelateOtherEntities"
        export const ClassifyOtherEntities = "HelpKindMessage.ClassifyOtherEntities"
        export const StoreInformationSharedByOtherEntities = "HelpKindMessage.StoreInformationSharedByOtherEntities"
        export const StoreInformationOnItsOwn = "HelpKindMessage.StoreInformationOnItsOwn"
        export const StorePartOfTheInformationOfAnotherEntity = "HelpKindMessage.StorePartOfTheInformationOfAnotherEntity"
        export const StorePartsOfInformationSharedByDifferentEntities = "HelpKindMessage.StorePartsOfInformationSharedByDifferentEntities"
        export const AutomaticallyByTheSystem = "HelpKindMessage.AutomaticallyByTheSystem"
        export const AndIsRarelyCreatedOrModified = "HelpKindMessage.AndIsRarelyCreatedOrModified"
        export const AndAreFrequentlyCreatedOrModified = "HelpKindMessage.AndAreFrequentlyCreatedOrModified"
    }
    
    export module HelpMessage {
        export const _0IsA1 = "HelpMessage._0IsA1"
        export const _0IsA1AndShows2 = "HelpMessage._0IsA1AndShows2"
        export const _0IsACalculated1 = "HelpMessage._0IsACalculated1"
        export const _0IsACollectionOfElements1 = "HelpMessage._0IsACollectionOfElements1"
        export const Amount = "HelpMessage.Amount"
        export const Any = "HelpMessage.Any"
        export const Appendices = "HelpMessage.Appendices"
        export const Buscador = "HelpMessage.Buscador"
        export const Call0Over1OfThe2 = "HelpMessage.Call0Over1OfThe2"
        export const Character = "HelpMessage.Character"
        export const Check = "HelpMessage.Check"
        export const ConstructsANew0 = "HelpMessage.ConstructsANew0"
        export const Date = "HelpMessage.Date"
        export const DateTime = "HelpMessage.DateTime"
        export const ExpressedIn = "HelpMessage.ExpressedIn"
        export const From0OfThe1 = "HelpMessage.From0OfThe1"
        export const FromMany0 = "HelpMessage.FromMany0"
        export const Help = "HelpMessage.Help"
        export const HelpNotLoaded = "HelpMessage.HelpNotLoaded"
        export const Integer = "HelpMessage.Integer"
        export const Key0NotFound = "HelpMessage.Key0NotFound"
        export const OfThe0 = "HelpMessage.OfThe0"
        export const OrNull = "HelpMessage.OrNull"
        export const Property0NotExistsInType1 = "HelpMessage.Property0NotExistsInType1"
        export const QueryOf0 = "HelpMessage.QueryOf0"
        export const RemovesThe0FromTheDatabase = "HelpMessage.RemovesThe0FromTheDatabase"
        export const Should = "HelpMessage.Should"
        export const String = "HelpMessage.String"
        export const The0 = "HelpMessage.The0"
        export const TheDatabaseVersion = "HelpMessage.TheDatabaseVersion"
        export const TheProperty0 = "HelpMessage.TheProperty0"
        export const Value = "HelpMessage.Value"
        export const ValueLike0 = "HelpMessage.ValueLike0"
        export const YourVersion = "HelpMessage.YourVersion"
        export const _0IsThePrimaryKeyOf1OfType2 = "HelpMessage._0IsThePrimaryKeyOf1OfType2"
        export const In0 = "HelpMessage.In0"
        export const Entities = "HelpMessage.Entities"
        export const SearchText = "HelpMessage.SearchText"
    }
    
    export module HelpPermissions {
        export const ViewHelp : Authorization.PermissionSymbol = { key: "HelpPermissions.ViewHelp" };
    }
    
    export module HelpSearchMessage {
        export const Search = "HelpSearchMessage.Search"
        export const _0ResultsFor1In2 = "HelpSearchMessage._0ResultsFor1In2"
        export const Results = "HelpSearchMessage.Results"
    }
    
    export module HelpSyntaxMessage {
        export const BoldText = "HelpSyntaxMessage.BoldText"
        export const ItalicText = "HelpSyntaxMessage.ItalicText"
        export const UnderlineText = "HelpSyntaxMessage.UnderlineText"
        export const StriketroughText = "HelpSyntaxMessage.StriketroughText"
        export const LinkToEntity = "HelpSyntaxMessage.LinkToEntity"
        export const LinkToProperty = "HelpSyntaxMessage.LinkToProperty"
        export const LinkToQuery = "HelpSyntaxMessage.LinkToQuery"
        export const LinkToOperation = "HelpSyntaxMessage.LinkToOperation"
        export const LinkToNamespace = "HelpSyntaxMessage.LinkToNamespace"
        export const ExernalLink = "HelpSyntaxMessage.ExernalLink"
        export const LinksAllowAnExtraParameterForTheText = "HelpSyntaxMessage.LinksAllowAnExtraParameterForTheText"
        export const Example = "HelpSyntaxMessage.Example"
        export const UnorderedListItem = "HelpSyntaxMessage.UnorderedListItem"
        export const OtherItem = "HelpSyntaxMessage.OtherItem"
        export const OrderedListItem = "HelpSyntaxMessage.OrderedListItem"
        export const TitleLevel = "HelpSyntaxMessage.TitleLevel"
        export const Title = "HelpSyntaxMessage.Title"
        export const Images = "HelpSyntaxMessage.Images"
        export const Texts = "HelpSyntaxMessage.Texts"
        export const Links = "HelpSyntaxMessage.Links"
        export const Lists = "HelpSyntaxMessage.Lists"
        export const InsertImage = "HelpSyntaxMessage.InsertImage"
        export const Options = "HelpSyntaxMessage.Options"
        export const Edit = "HelpSyntaxMessage.Edit"
        export const Save = "HelpSyntaxMessage.Save"
        export const Syntax = "HelpSyntaxMessage.Syntax"
        export const TranslateFrom = "HelpSyntaxMessage.TranslateFrom"
    }
    
    export const NamespaceHelpEntity: Entities.Type<NamespaceHelpEntity> = "NamespaceHelpEntity";
    export interface NamespaceHelpEntity extends Entities.Entity {
        name?: string;
        culture?: Basics.CultureInfoEntity;
        title?: string;
        description?: string;
    }
    
    export module NamespaceHelpOperation {
        export const Save : Entities.ExecuteSymbol<NamespaceHelpEntity> = { key: "NamespaceHelpOperation.Save" };
    }
    
    export const OperationHelpEntity: Entities.Type<OperationHelpEntity> = "OperationHelpEntity";
    export interface OperationHelpEntity extends Entities.Entity {
        operation?: Entities.OperationSymbol;
        culture?: Basics.CultureInfoEntity;
        description?: string;
    }
    
    export module OperationHelpOperation {
        export const Save : Entities.ExecuteSymbol<OperationHelpEntity> = { key: "OperationHelpOperation.Save" };
    }
    
    export const PropertyRouteHelpEntity: Entities.Type<PropertyRouteHelpEntity> = "PropertyRouteHelpEntity";
    export interface PropertyRouteHelpEntity extends Entities.EmbeddedEntity {
        property?: Basics.PropertyRouteEntity;
        description?: string;
    }
    
    export const QueryColumnHelpEntity: Entities.Type<QueryColumnHelpEntity> = "QueryColumnHelpEntity";
    export interface QueryColumnHelpEntity extends Entities.EmbeddedEntity {
        columnName?: string;
        description?: string;
    }
    
    export const QueryHelpEntity: Entities.Type<QueryHelpEntity> = "QueryHelpEntity";
    export interface QueryHelpEntity extends Entities.Entity {
        query?: Basics.QueryEntity;
        culture?: Basics.CultureInfoEntity;
        description?: string;
        columns?: Entities.MList<QueryColumnHelpEntity>;
        isEmpty?: boolean;
    }
    
    export module QueryHelpOperation {
        export const Save : Entities.ExecuteSymbol<QueryHelpEntity> = { key: "QueryHelpOperation.Save" };
    }
    
}

export namespace Isolation {

    export const IsolationEntity: Entities.Type<IsolationEntity> = "IsolationEntity";
    export interface IsolationEntity extends Entities.Entity {
        name?: string;
    }
    
    export module IsolationMessage {
        export const Entity0HasIsolation1ButCurrentIsolationIs2 = "IsolationMessage.Entity0HasIsolation1ButCurrentIsolationIs2"
        export const SelectAnIsolation = "IsolationMessage.SelectAnIsolation"
        export const Entity0HasIsolation1ButEntity2HasIsolation3 = "IsolationMessage.Entity0HasIsolation1ButEntity2HasIsolation3"
    }
    
    export const IsolationMixin: Entities.Type<IsolationMixin> = "IsolationMixin";
    export interface IsolationMixin extends Entities.MixinEntity {
        isolation?: Entities.Lite<IsolationEntity>;
    }
    
    export module IsolationOperation {
        export const Save : Entities.ExecuteSymbol<IsolationEntity> = { key: "IsolationOperation.Save" };
    }
    
}

export namespace Mailing {

    export module AsyncEmailSenderPermission {
        export const ViewAsyncEmailSenderPanel : Authorization.PermissionSymbol = { key: "AsyncEmailSenderPermission.ViewAsyncEmailSenderPanel" };
    }
    
    export enum CertFileType {
        CertFile,
        SignedFile,
    }
    
    export const ClientCertificationFileEntity: Entities.Type<ClientCertificationFileEntity> = "ClientCertificationFileEntity";
    export interface ClientCertificationFileEntity extends Entities.EmbeddedEntity {
        fullFilePath?: string;
        certFileType?: CertFileType;
    }
    
    export const EmailAddressEntity: Entities.Type<EmailAddressEntity> = "EmailAddressEntity";
    export interface EmailAddressEntity extends Entities.EmbeddedEntity {
        emailOwner?: Entities.Lite<IEmailOwnerEntity>;
        emailAddress?: string;
        displayName?: string;
    }
    
    export const EmailAttachmentEntity: Entities.Type<EmailAttachmentEntity> = "EmailAttachmentEntity";
    export interface EmailAttachmentEntity extends Entities.EmbeddedEntity {
        type?: EmailAttachmentType;
        file?: Files.FilePathEntity;
        contentId?: string;
    }
    
    export enum EmailAttachmentType {
        Attachment,
        LinkedResource,
    }
    
    export const EmailConfigurationEntity: Entities.Type<EmailConfigurationEntity> = "EmailConfigurationEntity";
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
        export const Attachment : Files.FileTypeSymbol = { key: "EmailFileType.Attachment" };
    }
    
    export const EmailMasterTemplateEntity: Entities.Type<EmailMasterTemplateEntity> = "EmailMasterTemplateEntity";
    export interface EmailMasterTemplateEntity extends Entities.Entity {
        name?: string;
        messages?: Entities.MList<EmailMasterTemplateMessageEntity>;
    }
    
    export const EmailMasterTemplateMessageEntity: Entities.Type<EmailMasterTemplateMessageEntity> = "EmailMasterTemplateMessageEntity";
    export interface EmailMasterTemplateMessageEntity extends Entities.EmbeddedEntity {
        masterTemplate?: EmailMasterTemplateEntity;
        cultureInfo?: Basics.CultureInfoEntity;
        text?: string;
    }
    
    export module EmailMasterTemplateOperation {
        export const Create : Entities.ConstructSymbol_Simple<EmailMasterTemplateEntity> = { key: "EmailMasterTemplateOperation.Create" };
        export const Save : Entities.ExecuteSymbol<EmailMasterTemplateEntity> = { key: "EmailMasterTemplateOperation.Save" };
    }
    
    export const EmailMessageEntity: Entities.Type<EmailMessageEntity> = "EmailMessageEntity";
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
        export const TheEmailMessageCannotBeSentFromState0 = "EmailMessageMessage.TheEmailMessageCannotBeSentFromState0"
        export const Message = "EmailMessageMessage.Message"
        export const Messages = "EmailMessageMessage.Messages"
        export const RemainingMessages = "EmailMessageMessage.RemainingMessages"
        export const ExceptionMessages = "EmailMessageMessage.ExceptionMessages"
        export const DefaultFromIsMandatory = "EmailMessageMessage.DefaultFromIsMandatory"
        export const From = "EmailMessageMessage.From"
        export const To = "EmailMessageMessage.To"
        export const Attachments = "EmailMessageMessage.Attachments"
    }
    
    export module EmailMessageOperation {
        export const Save : Entities.ExecuteSymbol<EmailMessageEntity> = { key: "EmailMessageOperation.Save" };
        export const ReadyToSend : Entities.ExecuteSymbol<EmailMessageEntity> = { key: "EmailMessageOperation.ReadyToSend" };
        export const Send : Entities.ExecuteSymbol<EmailMessageEntity> = { key: "EmailMessageOperation.Send" };
        export const ReSend : Entities.ConstructSymbol_From<EmailMessageEntity, EmailMessageEntity> = { key: "EmailMessageOperation.ReSend" };
        export const ReSendEmails : Entities.ConstructSymbol_FromMany<Processes.ProcessEntity, EmailMessageEntity> = { key: "EmailMessageOperation.ReSendEmails" };
        export const CreateMail : Entities.ConstructSymbol_Simple<EmailMessageEntity> = { key: "EmailMessageOperation.CreateMail" };
        export const CreateMailFromTemplate : Entities.ConstructSymbol_From<EmailMessageEntity, EmailTemplateEntity> = { key: "EmailMessageOperation.CreateMailFromTemplate" };
        export const Delete : Entities.DeleteSymbol<EmailMessageEntity> = { key: "EmailMessageOperation.Delete" };
    }
    
    export module EmailMessageProcess {
        export const SendEmails : Processes.ProcessAlgorithmSymbol = { key: "EmailMessageProcess.SendEmails" };
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
    
    export const EmailPackageEntity: Entities.Type<EmailPackageEntity> = "EmailPackageEntity";
    export interface EmailPackageEntity extends Entities.Entity, Processes.IProcessDataEntity {
        name?: string;
    }
    
    export const EmailReceptionInfoEntity: Entities.Type<EmailReceptionInfoEntity> = "EmailReceptionInfoEntity";
    export interface EmailReceptionInfoEntity extends Entities.EmbeddedEntity {
        uniqueId?: string;
        reception?: Entities.Lite<Pop3ReceptionEntity>;
        rawContent?: string;
        sentDate?: string;
        receivedDate?: string;
        deletionDate?: string;
    }
    
    export const EmailReceptionMixin: Entities.Type<EmailReceptionMixin> = "EmailReceptionMixin";
    export interface EmailReceptionMixin extends Entities.MixinEntity {
        receptionInfo?: EmailReceptionInfoEntity;
    }
    
    export const EmailRecipientEntity: Entities.Type<EmailRecipientEntity> = "EmailRecipientEntity";
    export interface EmailRecipientEntity extends EmailAddressEntity {
        kind?: EmailRecipientKind;
    }
    
    export enum EmailRecipientKind {
        To,
        Cc,
        Bcc,
    }
    
    export const EmailTemplateContactEntity: Entities.Type<EmailTemplateContactEntity> = "EmailTemplateContactEntity";
    export interface EmailTemplateContactEntity extends Entities.EmbeddedEntity {
        token?: UserAssets.QueryTokenEntity;
        emailAddress?: string;
        displayName?: string;
    }
    
    export const EmailTemplateEntity: Entities.Type<EmailTemplateEntity> = "EmailTemplateEntity";
    export interface EmailTemplateEntity extends Entities.Entity {
        name?: string;
        editableMessage?: boolean;
        disableAuthorization?: boolean;
        query?: Basics.QueryEntity;
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
        export const EndDateMustBeHigherThanStartDate = "EmailTemplateMessage.EndDateMustBeHigherThanStartDate"
        export const ThereAreNoMessagesForTheTemplate = "EmailTemplateMessage.ThereAreNoMessagesForTheTemplate"
        export const ThereMustBeAMessageFor0 = "EmailTemplateMessage.ThereMustBeAMessageFor0"
        export const TheresMoreThanOneMessageForTheSameLanguage = "EmailTemplateMessage.TheresMoreThanOneMessageForTheSameLanguage"
        export const TheTextMustContain0IndicatingReplacementPoint = "EmailTemplateMessage.TheTextMustContain0IndicatingReplacementPoint"
        export const TheTemplateIsAlreadyActive = "EmailTemplateMessage.TheTemplateIsAlreadyActive"
        export const TheTemplateIsAlreadyInactive = "EmailTemplateMessage.TheTemplateIsAlreadyInactive"
        export const SystemEmailShouldBeSetToAccessModel0 = "EmailTemplateMessage.SystemEmailShouldBeSetToAccessModel0"
        export const NewCulture = "EmailTemplateMessage.NewCulture"
        export const TokenOrEmailAddressMustBeSet = "EmailTemplateMessage.TokenOrEmailAddressMustBeSet"
        export const TokenAndEmailAddressCanNotBeSetAtTheSameTime = "EmailTemplateMessage.TokenAndEmailAddressCanNotBeSetAtTheSameTime"
        export const TokenMustBeA0 = "EmailTemplateMessage.TokenMustBeA0"
    }
    
    export const EmailTemplateMessageEntity: Entities.Type<EmailTemplateMessageEntity> = "EmailTemplateMessageEntity";
    export interface EmailTemplateMessageEntity extends Entities.EmbeddedEntity {
        template?: EmailTemplateEntity;
        cultureInfo?: Basics.CultureInfoEntity;
        text?: string;
        subject?: string;
    }
    
    export module EmailTemplateOperation {
        export const CreateEmailTemplateFromSystemEmail : Entities.ConstructSymbol_From<EmailTemplateEntity, SystemEmailEntity> = { key: "EmailTemplateOperation.CreateEmailTemplateFromSystemEmail" };
        export const Create : Entities.ConstructSymbol_Simple<EmailTemplateEntity> = { key: "EmailTemplateOperation.Create" };
        export const Save : Entities.ExecuteSymbol<EmailTemplateEntity> = { key: "EmailTemplateOperation.Save" };
        export const Enable : Entities.ExecuteSymbol<EmailTemplateEntity> = { key: "EmailTemplateOperation.Enable" };
        export const Disable : Entities.ExecuteSymbol<EmailTemplateEntity> = { key: "EmailTemplateOperation.Disable" };
    }
    
    export const EmailTemplateRecipientEntity: Entities.Type<EmailTemplateRecipientEntity> = "EmailTemplateRecipientEntity";
    export interface EmailTemplateRecipientEntity extends EmailTemplateContactEntity {
        kind?: EmailRecipientKind;
    }
    
    export module EmailTemplateViewMessage {
        export const InsertMessageContent = "EmailTemplateViewMessage.InsertMessageContent"
        export const Insert = "EmailTemplateViewMessage.Insert"
        export const Language = "EmailTemplateViewMessage.Language"
    }
    
    export interface IEmailOwnerEntity extends Entities.IEntity {
    }
    
    export const NewsletterDeliveryEntity: Entities.Type<NewsletterDeliveryEntity> = "NewsletterDeliveryEntity";
    export interface NewsletterDeliveryEntity extends Entities.Entity, Processes.IProcessLineDataEntity {
        sent?: boolean;
        sendDate?: string;
        recipient?: Entities.Lite<IEmailOwnerEntity>;
        newsletter?: Entities.Lite<NewsletterEntity>;
    }
    
    export const NewsletterEntity: Entities.Type<NewsletterEntity> = "NewsletterEntity";
    export interface NewsletterEntity extends Entities.Entity, Processes.IProcessDataEntity {
        name?: string;
        state?: NewsletterState;
        from?: string;
        displayFrom?: string;
        subject?: string;
        text?: string;
        query?: Basics.QueryEntity;
    }
    
    export module NewsletterOperation {
        export const Save : Entities.ExecuteSymbol<NewsletterEntity> = { key: "NewsletterOperation.Save" };
        export const Send : Entities.ConstructSymbol_From<Processes.ProcessEntity, NewsletterEntity> = { key: "NewsletterOperation.Send" };
        export const AddRecipients : Entities.ExecuteSymbol<NewsletterEntity> = { key: "NewsletterOperation.AddRecipients" };
        export const RemoveRecipients : Entities.ExecuteSymbol<NewsletterEntity> = { key: "NewsletterOperation.RemoveRecipients" };
        export const Clone : Entities.ConstructSymbol_From<NewsletterEntity, NewsletterEntity> = { key: "NewsletterOperation.Clone" };
    }
    
    export enum NewsletterState {
        Created,
        Saved,
        Sent,
    }
    
    export const Pop3ConfigurationEntity: Entities.Type<Pop3ConfigurationEntity> = "Pop3ConfigurationEntity";
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
        export const Save : Entities.ExecuteSymbol<Pop3ConfigurationEntity> = { key: "Pop3ConfigurationOperation.Save" };
        export const ReceiveEmails : Entities.ConstructSymbol_From<Pop3ReceptionEntity, Pop3ConfigurationEntity> = { key: "Pop3ConfigurationOperation.ReceiveEmails" };
    }
    
    export const Pop3ReceptionEntity: Entities.Type<Pop3ReceptionEntity> = "Pop3ReceptionEntity";
    export interface Pop3ReceptionEntity extends Entities.Entity {
        pop3Configuration?: Entities.Lite<Pop3ConfigurationEntity>;
        startDate?: string;
        endDate?: string;
        newEmails?: number;
        exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
    }
    
    export const Pop3ReceptionExceptionEntity: Entities.Type<Pop3ReceptionExceptionEntity> = "Pop3ReceptionExceptionEntity";
    export interface Pop3ReceptionExceptionEntity extends Entities.Entity {
        reception?: Entities.Lite<Pop3ReceptionEntity>;
        exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
    }
    
    export const SmtpConfigurationEntity: Entities.Type<SmtpConfigurationEntity> = "SmtpConfigurationEntity";
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
        export const Save : Entities.ExecuteSymbol<SmtpConfigurationEntity> = { key: "SmtpConfigurationOperation.Save" };
    }
    
    export const SmtpNetworkDeliveryEntity: Entities.Type<SmtpNetworkDeliveryEntity> = "SmtpNetworkDeliveryEntity";
    export interface SmtpNetworkDeliveryEntity extends Entities.EmbeddedEntity {
        host?: string;
        port?: number;
        username?: string;
        password?: string;
        useDefaultCredentials?: boolean;
        enableSSL?: boolean;
        clientCertificationFiles?: Entities.MList<ClientCertificationFileEntity>;
    }
    
    export const SystemEmailEntity: Entities.Type<SystemEmailEntity> = "SystemEmailEntity";
    export interface SystemEmailEntity extends Entities.Entity {
        fullClassName?: string;
    }
    
}

export namespace Map {

    export module MapMessage {
        export const Map = "MapMessage.Map"
        export const Namespace = "MapMessage.Namespace"
        export const TableSize = "MapMessage.TableSize"
        export const Columns = "MapMessage.Columns"
        export const Rows = "MapMessage.Rows"
        export const Press0ToExploreEachTable = "MapMessage.Press0ToExploreEachTable"
        export const Press0ToExploreStatesAndOperations = "MapMessage.Press0ToExploreStatesAndOperations"
        export const Filter = "MapMessage.Filter"
        export const Color = "MapMessage.Color"
        export const State = "MapMessage.State"
        export const StateColor = "MapMessage.StateColor"
    }
    
    export module MapPermission {
        export const ViewMap : Authorization.PermissionSymbol = { key: "MapPermission.ViewMap" };
    }
    
}

export namespace Migrations {

    export const CSharpMigrationEntity: Entities.Type<CSharpMigrationEntity> = "CSharpMigrationEntity";
    export interface CSharpMigrationEntity extends Entities.Entity {
        uniqueName?: string;
        executionDate?: string;
    }
    
    export const SqlMigrationEntity: Entities.Type<SqlMigrationEntity> = "SqlMigrationEntity";
    export interface SqlMigrationEntity extends Entities.Entity {
        versionNumber?: string;
    }
    
}

export namespace Notes {

    export const NoteEntity: Entities.Type<NoteEntity> = "NoteEntity";
    export interface NoteEntity extends Entities.Entity {
        title?: string;
        target?: Entities.Lite<Entities.Entity>;
        creationDate?: string;
        text?: string;
        createdBy?: Entities.Lite<Entities.Basics.IUserEntity>;
        noteType?: NoteTypeEntity;
    }
    
    export module NoteMessage {
        export const NewNote = "NoteMessage.NewNote"
        export const Note = "NoteMessage.Note"
        export const _note = "NoteMessage._note"
        export const _notes = "NoteMessage._notes"
        export const CreateNote = "NoteMessage.CreateNote"
        export const NoteCreated = "NoteMessage.NoteCreated"
        export const Notes = "NoteMessage.Notes"
        export const ViewNotes = "NoteMessage.ViewNotes"
    }
    
    export module NoteOperation {
        export const CreateNoteFromEntity : Entities.ConstructSymbol_From<NoteEntity, Entities.Entity> = { key: "NoteOperation.CreateNoteFromEntity" };
        export const Save : Entities.ExecuteSymbol<NoteEntity> = { key: "NoteOperation.Save" };
    }
    
    export const NoteTypeEntity: Entities.Type<NoteTypeEntity> = "NoteTypeEntity";
    export interface NoteTypeEntity extends Entities.Basics.SemiSymbol {
    }
    
    export module NoteTypeOperation {
        export const Save : Entities.ExecuteSymbol<NoteTypeEntity> = { key: "NoteTypeOperation.Save" };
    }
    
}

export namespace Omnibox {

    export module OmniboxMessage {
        export const No = "OmniboxMessage.No"
        export const NotFound = "OmniboxMessage.NotFound"
        export const Omnibox_DatabaseAccess = "OmniboxMessage.Omnibox_DatabaseAccess"
        export const Omnibox_Disambiguate = "OmniboxMessage.Omnibox_Disambiguate"
        export const Omnibox_Field = "OmniboxMessage.Omnibox_Field"
        export const Omnibox_Help = "OmniboxMessage.Omnibox_Help"
        export const Omnibox_MatchingOptions = "OmniboxMessage.Omnibox_MatchingOptions"
        export const Omnibox_Query = "OmniboxMessage.Omnibox_Query"
        export const Omnibox_Type = "OmniboxMessage.Omnibox_Type"
        export const Omnibox_UserChart = "OmniboxMessage.Omnibox_UserChart"
        export const Omnibox_UserQuery = "OmniboxMessage.Omnibox_UserQuery"
        export const Omnibox_Dashboard = "OmniboxMessage.Omnibox_Dashboard"
        export const Omnibox_Value = "OmniboxMessage.Omnibox_Value"
        export const Unknown = "OmniboxMessage.Unknown"
        export const Yes = "OmniboxMessage.Yes"
        export const ComplementWordsRegex = "OmniboxMessage.ComplementWordsRegex"
        export const Search = "OmniboxMessage.Search"
    }
    
}

export namespace Processes {

    export interface IProcessDataEntity extends Entities.IEntity {
    }
    
    export interface IProcessLineDataEntity extends Entities.IEntity {
    }
    
    export const PackageEntity: Entities.Type<PackageEntity> = "PackageEntity";
    export interface PackageEntity extends Entities.Entity, IProcessDataEntity {
        name?: string;
        operationArguments?: any;
    }
    
    export const PackageLineEntity: Entities.Type<PackageLineEntity> = "PackageLineEntity";
    export interface PackageLineEntity extends Entities.Entity, IProcessLineDataEntity {
        package?: Entities.Lite<PackageEntity>;
        target?: Entities.Entity;
        result?: Entities.Lite<Entities.Entity>;
        finishTime?: string;
    }
    
    export const PackageOperationEntity: Entities.Type<PackageOperationEntity> = "PackageOperationEntity";
    export interface PackageOperationEntity extends PackageEntity {
        operation?: Entities.OperationSymbol;
    }
    
    export module PackageOperationProcess {
        export const PackageOperation : ProcessAlgorithmSymbol = { key: "PackageOperationProcess.PackageOperation" };
    }
    
    export const ProcessAlgorithmSymbol: Entities.Type<ProcessAlgorithmSymbol> = "ProcessAlgorithmSymbol";
    export interface ProcessAlgorithmSymbol extends Entities.Symbol {
    }
    
    export const ProcessEntity: Entities.Type<ProcessEntity> = "ProcessEntity";
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
    
    export const ProcessExceptionLineEntity: Entities.Type<ProcessExceptionLineEntity> = "ProcessExceptionLineEntity";
    export interface ProcessExceptionLineEntity extends Entities.Entity {
        line?: Entities.Lite<IProcessLineDataEntity>;
        process?: Entities.Lite<ProcessEntity>;
        exception?: Entities.Lite<Entities.Basics.ExceptionEntity>;
    }
    
    export module ProcessMessage {
        export const Process0IsNotRunningAnymore = "ProcessMessage.Process0IsNotRunningAnymore"
        export const ProcessStartIsGreaterThanProcessEnd = "ProcessMessage.ProcessStartIsGreaterThanProcessEnd"
        export const ProcessStartIsNullButProcessEndIsNot = "ProcessMessage.ProcessStartIsNullButProcessEndIsNot"
        export const Lines = "ProcessMessage.Lines"
        export const LastProcess = "ProcessMessage.LastProcess"
        export const ExceptionLines = "ProcessMessage.ExceptionLines"
    }
    
    export module ProcessOperation {
        export const Plan : Entities.ExecuteSymbol<ProcessEntity> = { key: "ProcessOperation.Plan" };
        export const Save : Entities.ExecuteSymbol<ProcessEntity> = { key: "ProcessOperation.Save" };
        export const Cancel : Entities.ExecuteSymbol<ProcessEntity> = { key: "ProcessOperation.Cancel" };
        export const Execute : Entities.ExecuteSymbol<ProcessEntity> = { key: "ProcessOperation.Execute" };
        export const Suspend : Entities.ExecuteSymbol<ProcessEntity> = { key: "ProcessOperation.Suspend" };
        export const Retry : Entities.ConstructSymbol_From<ProcessEntity, ProcessEntity> = { key: "ProcessOperation.Retry" };
    }
    
    export module ProcessPermission {
        export const ViewProcessPanel : Authorization.PermissionSymbol = { key: "ProcessPermission.ViewProcessPanel" };
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
    
}

export namespace Profiler {

    export module ProfilerPermission {
        export const ViewTimeTracker : Authorization.PermissionSymbol = { key: "ProfilerPermission.ViewTimeTracker" };
        export const ViewHeavyProfiler : Authorization.PermissionSymbol = { key: "ProfilerPermission.ViewHeavyProfiler" };
        export const OverrideSessionTimeout : Authorization.PermissionSymbol = { key: "ProfilerPermission.OverrideSessionTimeout" };
    }
    
}

export namespace Scheduler {

    export const ApplicationEventLogEntity: Entities.Type<ApplicationEventLogEntity> = "ApplicationEventLogEntity";
    export interface ApplicationEventLogEntity extends Entities.Entity {
        machineName?: string;
        date?: string;
        globalEvent?: TypeEvent;
    }
    
    export const HolidayCalendarEntity: Entities.Type<HolidayCalendarEntity> = "HolidayCalendarEntity";
    export interface HolidayCalendarEntity extends Entities.Entity {
        name?: string;
        holidays?: Entities.MList<HolidayEntity>;
    }
    
    export module HolidayCalendarOperation {
        export const Save : Entities.ExecuteSymbol<HolidayCalendarEntity> = { key: "HolidayCalendarOperation.Save" };
        export const Delete : Entities.DeleteSymbol<HolidayCalendarEntity> = { key: "HolidayCalendarOperation.Delete" };
    }
    
    export const HolidayEntity: Entities.Type<HolidayEntity> = "HolidayEntity";
    export interface HolidayEntity extends Entities.EmbeddedEntity {
        date?: string;
        name?: string;
    }
    
    export interface IScheduleRuleEntity extends Entities.IEntity {
    }
    
    export interface ITaskEntity extends Entities.IEntity {
    }
    
    export const ScheduledTaskEntity: Entities.Type<ScheduledTaskEntity> = "ScheduledTaskEntity";
    export interface ScheduledTaskEntity extends Entities.Entity {
        rule?: IScheduleRuleEntity;
        task?: ITaskEntity;
        suspended?: boolean;
        machineName?: string;
        user?: Entities.Lite<Entities.Basics.IUserEntity>;
        applicationName?: string;
    }
    
    export const ScheduledTaskLogEntity: Entities.Type<ScheduledTaskLogEntity> = "ScheduledTaskLogEntity";
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
        export const Save : Entities.ExecuteSymbol<ScheduledTaskEntity> = { key: "ScheduledTaskOperation.Save" };
        export const Delete : Entities.DeleteSymbol<ScheduledTaskEntity> = { key: "ScheduledTaskOperation.Delete" };
    }
    
    export module SchedulerMessage {
        export const _0IsNotMultiple1 = "SchedulerMessage._0IsNotMultiple1"
        export const Each0Hours = "SchedulerMessage.Each0Hours"
        export const Each0Minutes = "SchedulerMessage.Each0Minutes"
        export const ScheduleRuleDailyEntity = "SchedulerMessage.ScheduleRuleDailyEntity"
        export const ScheduleRuleDailyDN_Everydayat = "SchedulerMessage.ScheduleRuleDailyDN_Everydayat"
        export const ScheduleRuleDayDN_StartingOn = "SchedulerMessage.ScheduleRuleDayDN_StartingOn"
        export const ScheduleRuleHourlyEntity = "SchedulerMessage.ScheduleRuleHourlyEntity"
        export const ScheduleRuleMinutelyEntity = "SchedulerMessage.ScheduleRuleMinutelyEntity"
        export const ScheduleRuleWeekDaysEntity = "SchedulerMessage.ScheduleRuleWeekDaysEntity"
        export const ScheduleRuleWeekDaysDN_AndHoliday = "SchedulerMessage.ScheduleRuleWeekDaysDN_AndHoliday"
        export const ScheduleRuleWeekDaysDN_At = "SchedulerMessage.ScheduleRuleWeekDaysDN_At"
        export const ScheduleRuleWeekDaysDN_ButHoliday = "SchedulerMessage.ScheduleRuleWeekDaysDN_ButHoliday"
        export const ScheduleRuleWeekDaysDN_Calendar = "SchedulerMessage.ScheduleRuleWeekDaysDN_Calendar"
        export const ScheduleRuleWeekDaysDN_F = "SchedulerMessage.ScheduleRuleWeekDaysDN_F"
        export const ScheduleRuleWeekDaysDN_Friday = "SchedulerMessage.ScheduleRuleWeekDaysDN_Friday"
        export const ScheduleRuleWeekDaysDN_Holiday = "SchedulerMessage.ScheduleRuleWeekDaysDN_Holiday"
        export const ScheduleRuleWeekDaysDN_M = "SchedulerMessage.ScheduleRuleWeekDaysDN_M"
        export const ScheduleRuleWeekDaysDN_Monday = "SchedulerMessage.ScheduleRuleWeekDaysDN_Monday"
        export const ScheduleRuleWeekDaysDN_S = "SchedulerMessage.ScheduleRuleWeekDaysDN_S"
        export const ScheduleRuleWeekDaysDN_Sa = "SchedulerMessage.ScheduleRuleWeekDaysDN_Sa"
        export const ScheduleRuleWeekDaysDN_Saturday = "SchedulerMessage.ScheduleRuleWeekDaysDN_Saturday"
        export const ScheduleRuleWeekDaysDN_Sunday = "SchedulerMessage.ScheduleRuleWeekDaysDN_Sunday"
        export const ScheduleRuleWeekDaysDN_T = "SchedulerMessage.ScheduleRuleWeekDaysDN_T"
        export const ScheduleRuleWeekDaysDN_Th = "SchedulerMessage.ScheduleRuleWeekDaysDN_Th"
        export const ScheduleRuleWeekDaysDN_Thursday = "SchedulerMessage.ScheduleRuleWeekDaysDN_Thursday"
        export const ScheduleRuleWeekDaysDN_Tuesday = "SchedulerMessage.ScheduleRuleWeekDaysDN_Tuesday"
        export const ScheduleRuleWeekDaysDN_W = "SchedulerMessage.ScheduleRuleWeekDaysDN_W"
        export const ScheduleRuleWeekDaysDN_Wednesday = "SchedulerMessage.ScheduleRuleWeekDaysDN_Wednesday"
        export const ScheduleRuleWeeklyEntity = "SchedulerMessage.ScheduleRuleWeeklyEntity"
        export const ScheduleRuleWeeklyDN_DayOfTheWeek = "SchedulerMessage.ScheduleRuleWeeklyDN_DayOfTheWeek"
    }
    
    export module SchedulerPermission {
        export const ViewSchedulerPanel : Authorization.PermissionSymbol = { key: "SchedulerPermission.ViewSchedulerPanel" };
    }
    
    export const ScheduleRuleDailyEntity: Entities.Type<ScheduleRuleDailyEntity> = "ScheduleRuleDailyEntity";
    export interface ScheduleRuleDailyEntity extends ScheduleRuleDayEntity {
    }
    
    export interface ScheduleRuleDayEntity extends Entities.Entity, IScheduleRuleEntity {
        startingOn?: string;
    }
    
    export const ScheduleRuleHourlyEntity: Entities.Type<ScheduleRuleHourlyEntity> = "ScheduleRuleHourlyEntity";
    export interface ScheduleRuleHourlyEntity extends Entities.Entity, IScheduleRuleEntity {
        eachHours?: number;
    }
    
    export const ScheduleRuleMinutelyEntity: Entities.Type<ScheduleRuleMinutelyEntity> = "ScheduleRuleMinutelyEntity";
    export interface ScheduleRuleMinutelyEntity extends Entities.Entity, IScheduleRuleEntity {
        eachMinutes?: number;
    }
    
    export const ScheduleRuleWeekDaysEntity: Entities.Type<ScheduleRuleWeekDaysEntity> = "ScheduleRuleWeekDaysEntity";
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
    
    export const ScheduleRuleWeeklyEntity: Entities.Type<ScheduleRuleWeeklyEntity> = "ScheduleRuleWeeklyEntity";
    export interface ScheduleRuleWeeklyEntity extends ScheduleRuleDayEntity {
        dayOfTheWeek?: External.DayOfWeek;
    }
    
    export const SimpleTaskSymbol: Entities.Type<SimpleTaskSymbol> = "SimpleTaskSymbol";
    export interface SimpleTaskSymbol extends Entities.Symbol, ITaskEntity {
    }
    
    export module TaskMessage {
        export const Execute = "TaskMessage.Execute"
        export const Executions = "TaskMessage.Executions"
        export const LastExecution = "TaskMessage.LastExecution"
    }
    
    export module TaskOperation {
        export const ExecuteSync : Entities.ConstructSymbol_From<Entities.IEntity, ITaskEntity> = { key: "TaskOperation.ExecuteSync" };
        export const ExecuteAsync : Entities.ExecuteSymbol<ITaskEntity> = { key: "TaskOperation.ExecuteAsync" };
    }
    
    export enum TypeEvent {
        Start,
        Stop,
    }
    
}

export namespace SMS {

    export enum MessageLengthExceeded {
        NotAllowed,
        Allowed,
        TextPruning,
    }
    
    export const MultipleSMSModel: Entities.Type<MultipleSMSModel> = "MultipleSMSModel";
    export interface MultipleSMSModel extends Entities.ModelEntity {
        message?: string;
        from?: string;
        certified?: boolean;
    }
    
    export const SMSConfigurationEntity: Entities.Type<SMSConfigurationEntity> = "SMSConfigurationEntity";
    export interface SMSConfigurationEntity extends Entities.EmbeddedEntity {
        defaultCulture?: Basics.CultureInfoEntity;
    }
    
    export module SmsMessage {
        export const Insert = "SmsMessage.Insert"
        export const Message = "SmsMessage.Message"
        export const RemainingCharacters = "SmsMessage.RemainingCharacters"
        export const RemoveNonValidCharacters = "SmsMessage.RemoveNonValidCharacters"
        export const StatusCanNotBeUpdatedForNonSentMessages = "SmsMessage.StatusCanNotBeUpdatedForNonSentMessages"
        export const TheTemplateMustBeActiveToConstructSMSMessages = "SmsMessage.TheTemplateMustBeActiveToConstructSMSMessages"
        export const TheTextForTheSMSMessageExceedsTheLengthLimit = "SmsMessage.TheTextForTheSMSMessageExceedsTheLengthLimit"
        export const Language = "SmsMessage.Language"
        export const Replacements = "SmsMessage.Replacements"
    }
    
    export const SMSMessageEntity: Entities.Type<SMSMessageEntity> = "SMSMessageEntity";
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
        export const Send : Entities.ExecuteSymbol<SMSMessageEntity> = { key: "SMSMessageOperation.Send" };
        export const UpdateStatus : Entities.ExecuteSymbol<SMSMessageEntity> = { key: "SMSMessageOperation.UpdateStatus" };
        export const CreateUpdateStatusPackage : Entities.ConstructSymbol_FromMany<Processes.ProcessEntity, SMSMessageEntity> = { key: "SMSMessageOperation.CreateUpdateStatusPackage" };
        export const CreateSMSFromSMSTemplate : Entities.ConstructSymbol_From<SMSMessageEntity, SMSTemplateEntity> = { key: "SMSMessageOperation.CreateSMSFromSMSTemplate" };
        export const CreateSMSWithTemplateFromEntity : Entities.ConstructSymbol_From<SMSMessageEntity, Entities.Entity> = { key: "SMSMessageOperation.CreateSMSWithTemplateFromEntity" };
        export const CreateSMSFromEntity : Entities.ConstructSymbol_From<SMSMessageEntity, Entities.Entity> = { key: "SMSMessageOperation.CreateSMSFromEntity" };
        export const SendSMSMessages : Entities.ConstructSymbol_FromMany<Processes.ProcessEntity, Entities.Entity> = { key: "SMSMessageOperation.SendSMSMessages" };
        export const SendSMSMessagesFromTemplate : Entities.ConstructSymbol_FromMany<Processes.ProcessEntity, Entities.Entity> = { key: "SMSMessageOperation.SendSMSMessagesFromTemplate" };
    }
    
    export module SMSMessageProcess {
        export const Send : Processes.ProcessAlgorithmSymbol = { key: "SMSMessageProcess.Send" };
        export const UpdateStatus : Processes.ProcessAlgorithmSymbol = { key: "SMSMessageProcess.UpdateStatus" };
    }
    
    export enum SMSMessageState {
        Created,
        Sent,
        Delivered,
        Failed,
    }
    
    export interface SMSPackageEntity extends Entities.Entity, Processes.IProcessDataEntity {
        name?: string;
    }
    
    export const SMSSendPackageEntity: Entities.Type<SMSSendPackageEntity> = "SMSSendPackageEntity";
    export interface SMSSendPackageEntity extends SMSPackageEntity {
    }
    
    export const SMSTemplateEntity: Entities.Type<SMSTemplateEntity> = "SMSTemplateEntity";
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
        export const EndDateMustBeHigherThanStartDate = "SMSTemplateMessage.EndDateMustBeHigherThanStartDate"
        export const ThereAreNoMessagesForTheTemplate = "SMSTemplateMessage.ThereAreNoMessagesForTheTemplate"
        export const ThereMustBeAMessageFor0 = "SMSTemplateMessage.ThereMustBeAMessageFor0"
        export const TheresMoreThanOneMessageForTheSameLanguage = "SMSTemplateMessage.TheresMoreThanOneMessageForTheSameLanguage"
        export const NewCulture = "SMSTemplateMessage.NewCulture"
    }
    
    export const SMSTemplateMessageEntity: Entities.Type<SMSTemplateMessageEntity> = "SMSTemplateMessageEntity";
    export interface SMSTemplateMessageEntity extends Entities.EmbeddedEntity {
        template?: SMSTemplateEntity;
        cultureInfo?: Basics.CultureInfoEntity;
        message?: string;
    }
    
    export module SMSTemplateOperation {
        export const Create : Entities.ConstructSymbol_Simple<SMSTemplateEntity> = { key: "SMSTemplateOperation.Create" };
        export const Save : Entities.ExecuteSymbol<SMSTemplateEntity> = { key: "SMSTemplateOperation.Save" };
    }
    
    export const SMSUpdatePackageEntity: Entities.Type<SMSUpdatePackageEntity> = "SMSUpdatePackageEntity";
    export interface SMSUpdatePackageEntity extends SMSPackageEntity {
    }
    
}

export namespace Templating {

    export module TemplateTokenMessage {
        export const NoColumnSelected = "TemplateTokenMessage.NoColumnSelected"
        export const YouCannotAddIfBlocksOnCollectionFields = "TemplateTokenMessage.YouCannotAddIfBlocksOnCollectionFields"
        export const YouHaveToAddTheElementTokenToUseForeachOnCollectionFields = "TemplateTokenMessage.YouHaveToAddTheElementTokenToUseForeachOnCollectionFields"
        export const YouCanOnlyAddForeachBlocksWithCollectionFields = "TemplateTokenMessage.YouCanOnlyAddForeachBlocksWithCollectionFields"
        export const YouCannotAddBlocksWithAllOrAny = "TemplateTokenMessage.YouCannotAddBlocksWithAllOrAny"
    }
    
}

export namespace Translation {

    export enum TranslatedCultureAction {
        Translate,
        Read,
    }
    
    export const TranslatedInstanceEntity: Entities.Type<TranslatedInstanceEntity> = "TranslatedInstanceEntity";
    export interface TranslatedInstanceEntity extends Entities.Entity {
        culture?: Basics.CultureInfoEntity;
        instance?: Entities.Lite<Entities.Entity>;
        propertyRoute?: Basics.PropertyRouteEntity;
        rowId?: string;
        translatedText?: string;
        originalText?: string;
    }
    
    export module TranslationJavascriptMessage {
        export const WrongTranslationToSubstitute = "TranslationJavascriptMessage.WrongTranslationToSubstitute"
        export const RightTranslation = "TranslationJavascriptMessage.RightTranslation"
        export const RememberChange = "TranslationJavascriptMessage.RememberChange"
    }
    
    export module TranslationMessage {
        export const RepeatedCultures0 = "TranslationMessage.RepeatedCultures0"
        export const CodeTranslations = "TranslationMessage.CodeTranslations"
        export const InstanceTranslations = "TranslationMessage.InstanceTranslations"
        export const Synchronize0In1 = "TranslationMessage.Synchronize0In1"
        export const View0In1 = "TranslationMessage.View0In1"
        export const AllLanguages = "TranslationMessage.AllLanguages"
        export const _0AlreadySynchronized = "TranslationMessage._0AlreadySynchronized"
        export const NothingToTranslate = "TranslationMessage.NothingToTranslate"
        export const All = "TranslationMessage.All"
        export const NothingToTranslateIn0 = "TranslationMessage.NothingToTranslateIn0"
        export const Sync = "TranslationMessage.Sync"
        export const View = "TranslationMessage.View"
        export const None = "TranslationMessage.None"
        export const Edit = "TranslationMessage.Edit"
        export const Member = "TranslationMessage.Member"
        export const Type = "TranslationMessage.Type"
        export const Instance = "TranslationMessage.Instance"
        export const Property = "TranslationMessage.Property"
        export const Save = "TranslationMessage.Save"
        export const Search = "TranslationMessage.Search"
        export const PressSearchForResults = "TranslationMessage.PressSearchForResults"
        export const NoResultsFound = "TranslationMessage.NoResultsFound"
    }
    
    export module TranslationPermission {
        export const TranslateCode : Authorization.PermissionSymbol = { key: "TranslationPermission.TranslateCode" };
        export const TranslateInstances : Authorization.PermissionSymbol = { key: "TranslationPermission.TranslateInstances" };
    }
    
    export const TranslationReplacementEntity: Entities.Type<TranslationReplacementEntity> = "TranslationReplacementEntity";
    export interface TranslationReplacementEntity extends Entities.Entity {
        cultureInfo?: Basics.CultureInfoEntity;
        wrongTranslation?: string;
        rightTranslation?: string;
    }
    
    export module TranslationReplacementOperation {
        export const Save : Entities.ExecuteSymbol<TranslationReplacementEntity> = { key: "TranslationReplacementOperation.Save" };
        export const Delete : Entities.DeleteSymbol<TranslationReplacementEntity> = { key: "TranslationReplacementOperation.Delete" };
    }
    
    export const TranslatorUserCultureEntity: Entities.Type<TranslatorUserCultureEntity> = "TranslatorUserCultureEntity";
    export interface TranslatorUserCultureEntity extends Entities.EmbeddedEntity {
        culture?: Basics.CultureInfoEntity;
        action?: TranslatedCultureAction;
    }
    
    export const TranslatorUserEntity: Entities.Type<TranslatorUserEntity> = "TranslatorUserEntity";
    export interface TranslatorUserEntity extends Entities.Entity {
        user?: Entities.Lite<Entities.Basics.IUserEntity>;
        cultures?: Entities.MList<TranslatorUserCultureEntity>;
    }
    
    export module TranslatorUserOperation {
        export const Save : Entities.ExecuteSymbol<TranslatorUserEntity> = { key: "TranslatorUserOperation.Save" };
        export const Delete : Entities.DeleteSymbol<TranslatorUserEntity> = { key: "TranslatorUserOperation.Delete" };
    }
    
}

export namespace UserAssets {

    export enum EntityAction {
        Identical,
        Different,
        New,
    }
    
    export interface IUserAssetEntity extends Entities.IEntity {
        guid?: string;
    }
    
    export const QueryTokenEntity: Entities.Type<QueryTokenEntity> = "QueryTokenEntity";
    export interface QueryTokenEntity extends Entities.EmbeddedEntity {
        tokenString?: string;
    }
    
    export module UserAssetMessage {
        export const ExportToXml = "UserAssetMessage.ExportToXml"
        export const ImportUserAssets = "UserAssetMessage.ImportUserAssets"
        export const ImportPreview = "UserAssetMessage.ImportPreview"
        export const SelectTheEntitiesToOverride = "UserAssetMessage.SelectTheEntitiesToOverride"
        export const SucessfullyImported = "UserAssetMessage.SucessfullyImported"
    }
    
    export module UserAssetPermission {
        export const UserAssetsToXML : Authorization.PermissionSymbol = { key: "UserAssetPermission.UserAssetsToXML" };
    }
    
    export const UserAssetPreviewLine: Entities.Type<UserAssetPreviewLine> = "UserAssetPreviewLine";
    export interface UserAssetPreviewLine extends Entities.EmbeddedEntity {
        type?: Entities.Basics.TypeEntity;
        text?: string;
        action?: EntityAction;
        overrideEntity?: boolean;
        guid?: string;
    }
    
    export const UserAssetPreviewModel: Entities.Type<UserAssetPreviewModel> = "UserAssetPreviewModel";
    export interface UserAssetPreviewModel extends Entities.ModelEntity {
        lines?: Entities.MList<UserAssetPreviewLine>;
    }
    
}

export namespace UserQueries {

    export const QueryColumnEntity: Entities.Type<QueryColumnEntity> = "QueryColumnEntity";
    export interface QueryColumnEntity extends Entities.EmbeddedEntity {
        token?: UserAssets.QueryTokenEntity;
        displayName?: string;
    }
    
    export const QueryFilterEntity: Entities.Type<QueryFilterEntity> = "QueryFilterEntity";
    export interface QueryFilterEntity extends Entities.EmbeddedEntity {
        token?: UserAssets.QueryTokenEntity;
        operation?: Entities.DynamicQuery.FilterOperation;
        valueString?: string;
    }
    
    export const QueryOrderEntity: Entities.Type<QueryOrderEntity> = "QueryOrderEntity";
    export interface QueryOrderEntity extends Entities.EmbeddedEntity {
        token?: UserAssets.QueryTokenEntity;
        orderType?: Entities.DynamicQuery.OrderType;
    }
    
    export const UserQueryEntity: Entities.Type<UserQueryEntity> = "UserQueryEntity";
    export interface UserQueryEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
        query?: Basics.QueryEntity;
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
        export const AreYouSureToRemove0 = "UserQueryMessage.AreYouSureToRemove0"
        export const Edit = "UserQueryMessage.Edit"
        export const MyQueries = "UserQueryMessage.MyQueries"
        export const RemoveUserQuery = "UserQueryMessage.RemoveUserQuery"
        export const _0ShouldBeEmptyIf1IsSet = "UserQueryMessage._0ShouldBeEmptyIf1IsSet"
        export const _0ShouldBeNullIf1Is2 = "UserQueryMessage._0ShouldBeNullIf1Is2"
        export const _0ShouldBeSetIf1Is2 = "UserQueryMessage._0ShouldBeSetIf1Is2"
        export const UserQueries_CreateNew = "UserQueryMessage.UserQueries_CreateNew"
        export const UserQueries_Edit = "UserQueryMessage.UserQueries_Edit"
        export const UserQueries_UserQueries = "UserQueryMessage.UserQueries_UserQueries"
        export const TheFilterOperation0isNotCompatibleWith1 = "UserQueryMessage.TheFilterOperation0isNotCompatibleWith1"
        export const _0IsNotFilterable = "UserQueryMessage._0IsNotFilterable"
        export const Use0ToFilterCurrentEntity = "UserQueryMessage.Use0ToFilterCurrentEntity"
        export const Preview = "UserQueryMessage.Preview"
    }
    
    export module UserQueryOperation {
        export const Save : Entities.ExecuteSymbol<UserQueryEntity> = { key: "UserQueryOperation.Save" };
        export const Delete : Entities.DeleteSymbol<UserQueryEntity> = { key: "UserQueryOperation.Delete" };
    }
    
    export module UserQueryPermission {
        export const ViewUserQuery : Authorization.PermissionSymbol = { key: "UserQueryPermission.ViewUserQuery" };
    }
    
}

export namespace ViewLog {

    export const ViewLogEntity: Entities.Type<ViewLogEntity> = "ViewLogEntity";
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

    export const SystemWordTemplateEntity: Entities.Type<SystemWordTemplateEntity> = "SystemWordTemplateEntity";
    export interface SystemWordTemplateEntity extends Entities.Entity {
        fullClassName?: string;
    }
    
    export const WordConverterSymbol: Entities.Type<WordConverterSymbol> = "WordConverterSymbol";
    export interface WordConverterSymbol extends Entities.Symbol {
    }
    
    export const WordTemplateEntity: Entities.Type<WordTemplateEntity> = "WordTemplateEntity";
    export interface WordTemplateEntity extends Entities.Entity {
        name?: string;
        query?: Basics.QueryEntity;
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
        export const ModelShouldBeSetToUseModel0 = "WordTemplateMessage.ModelShouldBeSetToUseModel0"
        export const Type0DoesNotHaveAPropertyWithName1 = "WordTemplateMessage.Type0DoesNotHaveAPropertyWithName1"
        export const ChooseAReportTemplate = "WordTemplateMessage.ChooseAReportTemplate"
    }
    
    export module WordTemplateOperation {
        export const Save : Entities.ExecuteSymbol<WordTemplateEntity> = { key: "WordTemplateOperation.Save" };
        export const CreateWordReport : Entities.ExecuteSymbol<WordTemplateEntity> = { key: "WordTemplateOperation.CreateWordReport" };
        export const CreateWordTemplateFromSystemWordTemplate : Entities.ConstructSymbol_From<WordTemplateEntity, SystemWordTemplateEntity> = { key: "WordTemplateOperation.CreateWordTemplateFromSystemWordTemplate" };
    }
    
    export module WordTemplatePermission {
        export const GenerateReport : Authorization.PermissionSymbol = { key: "WordTemplatePermission.GenerateReport" };
    }
    
    export const WordTransformerSymbol: Entities.Type<WordTransformerSymbol> = "WordTransformerSymbol";
    export interface WordTransformerSymbol extends Entities.Symbol {
    }
    
}

