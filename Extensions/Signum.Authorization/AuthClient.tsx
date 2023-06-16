import * as React from "react";
import { RouteObject, Location } from 'react-router'
import * as Services from '@framework/Services';
import { ImportComponent } from '@framework/ImportComponent'
import LoginPage, { LoginWithWindowsButton } from "./Login/LoginPage";
import * as AppContext from "@framework/AppContext";
import { ajaxGet, ajaxPost, ServiceError } from "@framework/Services";
import { is } from '@framework/Signum.Entities';
import { ifError } from "@framework/Globals";
import { Cookies } from "@framework/Cookies";
import { tryGetTypeInfo } from "@framework/Reflection";
import { UserEntity} from './Signum.Authorization';
import { PermissionSymbol } from "@framework/Signum.Basics";

export function startPublic(options: { routes: RouteObject[], userTicket: boolean, windowsAuthentication: boolean, resetPassword: boolean, notifyLogout: boolean }) {
  Options.userTicket = options.userTicket;
  Options.windowsAuthentication = options.windowsAuthentication;
  Options.resetPassword = options.resetPassword;

  if (Options.userTicket) {
    if (!authenticators.contains(loginFromCookie))
      throw new Error("call AuthClient.registerUserTicketAuthenticator in MainPublic.tsx before AuthClient.autoLogin");
  }

  if (Options.windowsAuthentication) {
    if (!authenticators.contains(loginWindowsAuthentication))
      throw new Error("call AuthClient.registerWindowsAuthenticator in MainPublic.tsx before AuthClient.autoLogin");

    LoginPage.customLoginButtons = () => <LoginWithWindowsButton />;
  }

  options.routes.push({ path: "/auth/login", element: <ImportComponent onImport={() => import("./Login/LoginPage")} /> });
  options.routes.push({ path: "/auth/changePassword", element: <ImportComponent onImport={() => import("./Login/ChangePasswordPage")} /> });
  options.routes.push({ path: "/auth/changePasswordSuccess", element: <ImportComponent onImport={() => import("./Login/ChangePasswordSuccessPage")} /> });
  options.routes.push({ path: "/auth/resetPassword", element: <ImportComponent onImport={() => import("./Login/ResetPassword")} /> });
  options.routes.push({ path: "/auth/forgotPasswordEmail", element: <ImportComponent onImport={() => import("./Login/ForgotPasswordEmailPage")} /> });

  if (options.notifyLogout) {
    notifyLogout = options.notifyLogout;

    window.addEventListener("storage", se => {
      if (se.key == 'requestLogout' + Services.SessionSharing.getAppName()) {

        var userName = se.newValue!.before("&&");

        var cu = currentUser();
        if (cu?.userName == userName)
          logoutInternal();
      }
    });
  }
}

export function assertPermissionAuthorized(permission: PermissionSymbol | string) {
  var key = (permission as PermissionSymbol).key ?? permission as string;
  if (!isPermissionAuthorized(key))
    throw new Error(`Permission ${key} is denied`);
}

export function isPermissionAuthorized(permission: PermissionSymbol | string) {
  var key = (permission as PermissionSymbol).key ?? permission as string;
  const type = tryGetTypeInfo(key.before("."));

  if (!type)
    return false;

  const member = type.members[key.after(".")];
  if (!member)
    return false;

  return true;
}

export namespace Options {
  export let AuthHeader = "Authorization";
}

var notifyLogout: boolean;

export const authenticators: Array<() => Promise<AuthenticatedUser | undefined>> = [];


export function loginFromCookie(): Promise<AuthenticatedUser | undefined> {
  var myCookie = Options.getCookie();

  if (!myCookie) {
    return Promise.resolve(undefined);
  }

  return API.loginFromCookie().then(au => {
    if (au) {
      console.log("loginFromCookie");
    }
    else {
      Options.removeCookie();
    }
    return au;
  });
}

export function loginWindowsAuthentication(): Promise<AuthenticatedUser | undefined> {

  if (Options.disableWindowsAuthentication)
    return Promise.resolve(undefined);

  return API.loginWindowsAuthentication(false).then(au => {
    au && console.log("loginWindowsAuthentication");
    return au;
  }).catch(() => undefined);
}

export async function authenticate(): Promise<AuthenticatedUser | undefined> {
  for (let i = 0; i < authenticators.length; i++) {
    let aUser = await authenticators[i]();
    if (aUser)
      return aUser;
  }

  return undefined;
}

export interface AuthenticatedUser {
  userEntity: UserEntity;
  token: string;
  authenticationType: AuthenticationType;
}

export function currentUser(): UserEntity {
  return AppContext.currentUser as UserEntity;
}

export function logout() {
  var user = currentUser();
  if (user == null)
    return;

  Options.removeCookie();

  API.logout().then(() => {
    logoutInternal();
    logoutOtherTabs(user);
  });
}

function logoutInternal() {
  setAuthToken(undefined, undefined);
  setCurrentUser(undefined);
  Options.disableWindowsAuthentication = true;
  Options.onLogout();
}

Services.AuthTokenFilter.addAuthToken = addAuthToken;

export const onCurrentUserChanged: Array<(newUser: UserEntity | undefined, avoidReRender?: boolean) => void> = [];

export function setCurrentUser(user: UserEntity | undefined, avoidReRender?: boolean) {

  const changed = !is(AppContext.currentUser, user, true);

  AppContext.setCurrentUser(user);

  if (changed)
    onCurrentUserChanged.forEach(f => f(user, avoidReRender));
}

export function addAuthToken(options: Services.AjaxOptions, makeCall: () => Promise<Response>): Promise<Response> {

  const token = getAuthToken();

  if (!token)
    return makeCall();

  if (options.headers == undefined)
    options.headers = {};

  options.headers[Options.AuthHeader] = "Bearer " + token;

  return makeCall()
    .then(r => {
      var newToken = r.headers.get("New_Token");
      if (newToken) {
        setAuthToken(newToken, getAuthenticationType());
        API.fetchCurrentUser()
          .then(cu => setCurrentUser(cu));
      }

      return r;
    }, ifError<ServiceError, Response>(ServiceError, e => {

      if (e.httpError.exceptionType?.endsWith(".AuthenticationException")) {
        setAuthToken(undefined, undefined);
        setCurrentUser(undefined);
        AppContext.resetUI();
        AppContext.navigate("/auth/login");
      }

      throw e;
    }));
}

export function getAuthToken(): string | undefined {
  return sessionStorage.getItem("authToken") ?? undefined;
}

export function getAuthenticationType(): AuthenticationType | undefined {
  return sessionStorage.getItem("authenticationType") as AuthenticationType | null ?? undefined;
}

export function setAuthToken(authToken: string | undefined, authenticationType: AuthenticationType | undefined): void {
  sessionStorage.setItem("authToken", authToken ?? "");
  sessionStorage.setItem("authenticationType", authenticationType ?? "");
}

export function registerUserTicketAuthenticator() {
  authenticators.push(loginFromCookie);
}

/* Install and enable Windows authentication in IIS https://docs.microsoft.com/en-us/aspnet/core/security/authentication/windowsauth?view=aspnetcore-2.2&tabs=visual-studio */
export function registerWindowsAuthenticator() {
  Options.AuthHeader = "Signum_Authorization"; //Authorization is used by IIS with Negotiate prefix
  authenticators.push(loginWindowsAuthentication);
}

export function autoLogin(): Promise<UserEntity | undefined> {
  if (AppContext.currentUser)
    return Promise.resolve(AppContext.currentUser as UserEntity);

  if (getAuthToken())
    return API.fetchCurrentUser().then(u => {
      setCurrentUser(u);
      AppContext.resetUI();
      return u;
    }, e => {
      console.error(e);
      setAuthToken(undefined, undefined);
      return undefined;
    });

  return new Promise<undefined>((resolve) => window.setTimeout(() => resolve(undefined), 500))
    .then(() => {
      if (getAuthToken()) {
        return API.fetchCurrentUser()
          .then(u => {
            setCurrentUser(u);
            AppContext.resetUI();
            return u;
          }, e => {
            console.error(e);
            setAuthToken(undefined, undefined);
            return undefined;
          });
      } else {
        return authenticate()
          .then(au => {
            if (!au) {
              return undefined;
            } else {
              setAuthToken(au.token, au.authenticationType);
              setCurrentUser(au.userEntity);
              AppContext.resetUI();
              return au.userEntity;
            }
          });
      }
    });
}

export function logoutOtherTabs(user: UserEntity) {
  if (notifyLogout)
    localStorage.setItem('requestLogout' + Services.SessionSharing.getAppName(), user.userName + "&&" + new Date().toString());
}

export namespace Options {

  export function getCookie(): string | null { return Cookies.get("sfUser"); }
  export function removeCookie() { return Cookies.remove("sfUser", "/", document.location.hostname); }

  export let onLogout: () => void = () => {
    throw new Error("onLogout should be defined (check MainPublic.tsx in Southwind)");
  }

  export let onLogin: (url?: string) => void = () => {
    throw new Error("onLogin should be defined (check MainPublic.tsx in Southwind)");
  }

  export let disableWindowsAuthentication: boolean;
  export let windowsAuthentication: boolean;
  export let userTicket: boolean;
  export let resetPassword: boolean;
}

export type AuthenticationType = "database" | "resetPassword" | "changePassword" | "api-key" | "azureAD" | "cookie" | "windows";

export module API {
  export interface LoginRequest {
    userName: string;
    password: string;
    rememberMe?: boolean;
  }

  export interface LoginResponse {
    authenticationType: AuthenticationType;
    message?: string;
    token: string;
    userEntity: UserEntity;
  }

  export function login(loginRequest: LoginRequest): Promise<LoginResponse> {
    return ajaxPost({ url: "/api/auth/login" }, loginRequest);
  }

  export function loginFromCookie(): Promise<LoginResponse | undefined> {
    return ajaxPost({ url: "/api/auth/loginFromCookie", avoidAuthToken: true }, undefined);
  }

  export function loginWindowsAuthentication(throwError: boolean): Promise<LoginResponse | undefined> {
    return ajaxPost({ url: `/api/auth/loginWindowsAuthentication?throwError=${throwError}`, avoidAuthToken: true }, undefined);
  }

  export function loginWithAzureAD(jwt: string, throwErrors: boolean): Promise<LoginResponse | undefined> {
    return ajaxPost({ url: "/api/auth/loginWithAzureAD?throwErrors=" + throwErrors, avoidAuthToken: true }, jwt);
  }

  export interface ChangePasswordRequest {
    oldPassword: string;
    newPassword: string;
  }

  export interface ForgotPasswordEmailRequest {
    email: string;
  }

  export interface ResetPasswordRequest {
    code: string;
    newPassword: string;
  }

  export function forgotPasswordEmail(request: ForgotPasswordEmailRequest): Promise<string> {
    return ajaxPost({ url: "/api/auth/forgotPasswordEmail" }, request);
  }

  export function resetPassword(request: ResetPasswordRequest): Promise<LoginResponse> {
    return ajaxPost({ url: "/api/auth/resetPassword" }, request);
  }

  export function changePassword(request: ChangePasswordRequest): Promise<LoginResponse> {
    return ajaxPost({ url: "/api/auth/changePassword" }, request);
  }

  export function fetchCurrentUser(refreshToken: boolean = false): Promise<UserEntity> {
    return ajaxGet({ url: "/api/auth/currentUser" + (refreshToken ? "?refreshToken=true" : ""), cache: "no-cache" });
  }

  export function logout(): Promise<void> {
    return ajaxPost({ url: "/api/auth/logout" }, undefined);
  }
}
