import { RouteObject } from 'react-router';
import * as Services from '@framework/Services';
import { UserEntity } from './Signum.Authorization';
import { PermissionSymbol } from "@framework/Signum.Basics";
export declare function startPublic(options: {
    routes: RouteObject[];
    userTicket: boolean;
    windowsAuthentication: boolean;
    resetPassword: boolean;
    notifyLogout: boolean;
}): void;
export declare function assertPermissionAuthorized(permission: PermissionSymbol | string): void;
export declare function isPermissionAuthorized(permission: PermissionSymbol | string): boolean;
export declare namespace Options {
    let AuthHeader: string;
}
export declare const authenticators: Array<() => Promise<AuthenticatedUser | undefined>>;
export declare function loginFromCookie(): Promise<AuthenticatedUser | undefined>;
export declare function loginWindowsAuthentication(): Promise<AuthenticatedUser | undefined>;
export declare function authenticate(): Promise<AuthenticatedUser | undefined>;
export interface AuthenticatedUser {
    userEntity: UserEntity;
    token: string;
    authenticationType: AuthenticationType;
}
export declare function currentUser(): UserEntity;
export declare function logout(): void;
export declare const onCurrentUserChanged: Array<(newUser: UserEntity | undefined, avoidReRender?: boolean) => void>;
export declare function setCurrentUser(user: UserEntity | undefined, avoidReRender?: boolean): void;
export declare function addAuthToken(options: Services.AjaxOptions, makeCall: () => Promise<Response>): Promise<Response>;
export declare function getAuthToken(): string | undefined;
export declare function getAuthenticationType(): AuthenticationType | undefined;
export declare function setAuthToken(authToken: string | undefined, authenticationType: AuthenticationType | undefined): void;
export declare function registerUserTicketAuthenticator(): void;
export declare function registerWindowsAuthenticator(): void;
export declare function autoLogin(): Promise<UserEntity | undefined>;
export declare function logoutOtherTabs(user: UserEntity): void;
export declare namespace Options {
    function getCookie(): string | null;
    function removeCookie(): any;
    let onLogout: () => void;
    let onLogin: (url?: string) => void;
    let disableWindowsAuthentication: boolean;
    let windowsAuthentication: boolean;
    let userTicket: boolean;
    let resetPassword: boolean;
}
export type AuthenticationType = "database" | "resetPassword" | "changePassword" | "api-key" | "azureAD" | "cookie" | "windows";
export declare module API {
    interface LoginRequest {
        userName: string;
        password: string;
        rememberMe?: boolean;
    }
    interface LoginResponse {
        authenticationType: AuthenticationType;
        message?: string;
        token: string;
        userEntity: UserEntity;
    }
    function login(loginRequest: LoginRequest): Promise<LoginResponse>;
    function loginFromCookie(): Promise<LoginResponse | undefined>;
    function loginWindowsAuthentication(throwError: boolean): Promise<LoginResponse | undefined>;
    function loginWithAzureAD(jwt: string, throwErrors: boolean): Promise<LoginResponse | undefined>;
    interface ChangePasswordRequest {
        oldPassword: string;
        newPassword: string;
    }
    interface ForgotPasswordEmailRequest {
        email: string;
    }
    interface ResetPasswordRequest {
        code: string;
        newPassword: string;
    }
    function forgotPasswordEmail(request: ForgotPasswordEmailRequest): Promise<string>;
    function resetPassword(request: ResetPasswordRequest): Promise<LoginResponse>;
    function changePassword(request: ChangePasswordRequest): Promise<LoginResponse>;
    function fetchCurrentUser(refreshToken?: boolean): Promise<UserEntity>;
    function logout(): Promise<void>;
}
//# sourceMappingURL=AuthClient.d.ts.map