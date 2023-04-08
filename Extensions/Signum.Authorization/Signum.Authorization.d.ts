import { MessageKey, Type, EnumType } from '../../Signum/React/Reflection';
import * as Entities from '../../Signum/React/Signum.Entities';
import * as Basics from '../../Signum/React/Signum.Basics';
import * as Security from '../../Signum/React/Signum.Security';
import * as Signum from '../../Signum/React/Signum.Entities.Basics';
import * as Operations from '../../Signum/React/Signum.Operations';
import * as Rules from './Rules/Signum.Authorization.Rules';
export interface UserEntity {
    newPassword: string;
}
export declare module AuthMessage {
    const NotAuthorizedTo0The1WithId2: MessageKey;
    const NotAuthorizedToRetrieve0: MessageKey;
}
export declare module LoginAuthMessage {
    const ThePasswordMustHaveAtLeast0Characters: MessageKey;
    const NotUserLogged: MessageKey;
    const Username0IsNotValid: MessageKey;
    const User0IsDisabled: MessageKey;
    const IncorrectPassword: MessageKey;
    const Login: MessageKey;
    const MyProfile: MessageKey;
    const Password: MessageKey;
    const ChangePassword: MessageKey;
    const SwitchUser: MessageKey;
    const Logout: MessageKey;
    const EnterYourUserNameAndPassword: MessageKey;
    const Username: MessageKey;
    const EMailAddress: MessageKey;
    const EmailAddressOrUsername: MessageKey;
    const RememberMe: MessageKey;
    const IHaveForgottenMyPassword: MessageKey;
    const ShowLoginForm: MessageKey;
    const LoginWithWindowsUser: MessageKey;
    const NoWindowsUserFound: MessageKey;
    const LooksLikeYourWindowsUserIsNotAllowedToUseThisApplication: MessageKey;
    const IForgotMyPassword: MessageKey;
    const EnterYourUserEmail: MessageKey;
    const SendEmail: MessageKey;
    const GiveUsYourUserEmailToResetYourPassword: MessageKey;
    const RequestAccepted: MessageKey;
    const PasswordMustHaveAValue: MessageKey;
    const PasswordsAreDifferent: MessageKey;
    const PasswordChanged: MessageKey;
    const PasswordHasBeenChangedSuccessfully: MessageKey;
    const NewPassword: MessageKey;
    const EnterTheNewPassword: MessageKey;
    const ConfirmNewPassword: MessageKey;
    const EnterActualPasswordAndNewOne: MessageKey;
    const CurrentPassword: MessageKey;
    const WeHaveSentYouAnEmailToResetYourPassword: MessageKey;
    const UserNameMustHaveAValue: MessageKey;
    const InvalidUsernameOrPassword: MessageKey;
    const InvalidUsername: MessageKey;
    const InvalidPassword: MessageKey;
    const AnErrorOccurredRequestNotProcessed: MessageKey;
    const TheUserIsNotLongerInTheDatabase: MessageKey;
    const Register0: MessageKey;
    const Success: MessageKey;
    const _0HasBeenSucessfullyAssociatedWithUser1InThisDevice: MessageKey;
    const TryToLogInWithIt: MessageKey;
    const LoginWith0: MessageKey;
    const SignInWithMicrosoft: MessageKey;
}
export declare const MergeStrategy: EnumType<MergeStrategy>;
export type MergeStrategy = "Union" | "Intersection";
export declare const RoleEntity: Type<RoleEntity>;
export interface RoleEntity extends Entities.Entity {
    Type: "Role";
    name: string;
    mergeStrategy: MergeStrategy;
    isTrivialMerge: boolean;
    inheritsFrom: Entities.MList<Entities.Lite<RoleEntity>>;
    description: string | null;
}
export declare module RoleOperation {
    const Save: Operations.ExecuteSymbol<RoleEntity>;
    const Delete: Operations.DeleteSymbol<RoleEntity>;
}
export declare const UserEntity: Type<UserEntity>;
export interface UserEntity extends Entities.Entity, Basics.IEmailOwnerEntity, Security.IUserEntity {
    Type: "User";
    userName: string;
    passwordHash: string | null;
    role: Entities.Lite<RoleEntity>;
    email: string | null;
    cultureInfo: Signum.CultureInfoEntity | null;
    disabledOn: string | null;
    state: UserState;
    loginFailedCounter: number;
}
export declare const UserLiteModel: Type<UserLiteModel>;
export interface UserLiteModel extends Entities.ModelEntity {
    Type: "UserLiteModel";
    userName: string;
    toStringValue: string | null;
    oID: string | null;
    sID: string | null;
}
export declare module UserOperation {
    const Create: Operations.ConstructSymbol_Simple<UserEntity>;
    const Save: Operations.ExecuteSymbol<UserEntity>;
    const Reactivate: Operations.ExecuteSymbol<UserEntity>;
    const Deactivate: Operations.ExecuteSymbol<UserEntity>;
    const SetPassword: Operations.ExecuteSymbol<UserEntity>;
    const Delete: Operations.DeleteSymbol<UserEntity>;
}
export declare const UserState: EnumType<UserState>;
export type UserState = "New" | "Active" | "Deactivated";
export declare module UserTypeCondition {
    const DeactivatedUsers: Rules.TypeConditionSymbol;
}
//# sourceMappingURL=Signum.Authorization.d.ts.map