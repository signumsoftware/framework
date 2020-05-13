import * as React from "react";
import * as Services from '@framework/Services';
import { ImportRoute } from "@framework/AsyncImport";
import Login, { LoginWithWindowsButton } from "./Login/Login";
import * as AppContext from "@framework/AppContext";
import { UserEntity, PermissionSymbol } from "./Signum.Entities.Authorization";
import { ajaxPost, ajaxGet, ServiceError } from "@framework/Services";
import { is } from "@framework/Signum.Entities";
import { ifError } from "@framework/Globals";
import { tryGetTypeInfo } from "@framework/Reflection";

export function startPublic(options: { routes: JSX.Element[], userTicket: boolean, windowsAuthentication: boolean, resetPassword: boolean, notifyLogout: boolean }) {
  Options.userTicket = options.userTicket;
  Options.windowsAuthentication = options.windowsAuthentication;
  Options.resetPassword = options.resetPassword;

  if (Options.userTicket) {
    if (!authenticators.contains(loginFromCookie))
      throw new Error("call AuthClient.registerUserTicketAuthenticator in Main.tsx before AuthClient.autoLogin");
  }

  if (Options.windowsAuthentication) {
    if (!authenticators.contains(loginWindowsAuthentication))
      throw new Error("call AuthClient.registerWindowsAuthenticator in Main.tsx before AuthClient.autoLogin");

    Login.customLoginButtons = () => <LoginWithWindowsButton />;
  }

  options.routes.push(<ImportRoute path="~/auth/login" onImportModule={() => import("./Login/Login")} />);
  options.routes.push(<ImportRoute path="~/auth/changePassword" onImportModule={() => import("./Login/ChangePassword")} />);
  options.routes.push(<ImportRoute path="~/auth/changePasswordSuccess" onImportModule={() => import("./Login/ChangePasswordSuccess")} />);
  options.routes.push(<ImportRoute path="~/auth/resetPassword" onImportModule={() => import("./Login/ResetPassword")} />);
  options.routes.push(<ImportRoute path="~/auth/forgotPasswordEmail" onImportModule={() => import("./Login/ForgotPasswordEmail")} />);

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

var notifyLogout: boolean;


export const authenticators: Array<() => Promise<AuthenticatedUser | undefined>> = [];

export function loginFromCookie(): Promise<AuthenticatedUser | undefined> {

  var myCookie = getCookie("sfUser");

  if (!myCookie) {
    return new Promise<undefined>(resolve => resolve());
  }
  else {
    return API.loginFromCookie().then(au => {
      au && console.log("loginFromCookie");
      return au;
    });
  }
}

export function loginWindowsAuthentication(): Promise<AuthenticatedUser | undefined> {

  if (Options.disableWindowsAuthentication)
    return Promise.resolve(undefined);

  return API.loginWindowsAuthentication(false).then(au => {
    au && console.log("loginWindowsAuthentication");
    return au;
  }).catch(() => undefined);
}

function getCookie(name: string) {
  var dc = document.cookie;
  var prefix = name + "=";
  var begin = dc.indexOf("; " + prefix);

  if (begin == -1) {
    begin = dc.indexOf(prefix);
    if (begin != 0) return null;
  }

  var end = document.cookie.indexOf(";", begin + 2);
  if (end == -1) {
    end = dc.length;
  }

  return decodeURI(dc.substring(begin + prefix.length, end));
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
  authenticationType: string;
}

export function currentUser(): UserEntity {
  return AppContext.currentUser as UserEntity;
}

export function logout() {
  var user = currentUser();
  if (user == null)
    return;

  API.logout().then(() => {
    logoutInternal();
    logoutOtherTabs(user);
  }).done();
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

  options.headers["Signum_Authorization"] = "Bearer " + token;

  return makeCall()
    .then(r => {
      var newToken = r.headers.get("New_Token");
      if (newToken) {
        setAuthToken(newToken, getAuthenticationType());
        API.fetchCurrentUser()
          .then(cu => setCurrentUser(cu))
          .done();
      }

      return r;
    }, ifError<ServiceError, Response>(ServiceError, e => {

      if (e.httpError.exceptionType?.endsWith(".AuthenticationException")) {
        setAuthToken(undefined, undefined);
        AppContext.history?.push("~/auth/login");
      }

      throw e;
    }));
}

export function getAuthToken(): string | undefined {
  return sessionStorage.getItem("authToken") ?? undefined;
}

export function getAuthenticationType(): string | undefined {
  return sessionStorage.getItem("authenticationType") ?? undefined;
}

export function setAuthToken(authToken: string | undefined, authenticationType: string | undefined): void {
  sessionStorage.setItem("authToken", authToken ?? "");
  sessionStorage.setItem("authenticationType", authenticationType ?? "");
}

export function registerUserTicketAuthenticator() {
  authenticators.push(loginFromCookie);
}

/* Install and enable Windows authentication in IIS https://docs.microsoft.com/en-us/aspnet/core/security/authentication/windowsauth?view=aspnetcore-2.2&tabs=visual-studio */
export function registerWindowsAuthenticator() {
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
    });

  return new Promise<UserEntity>((resolve) => {
    setTimeout(() => {
      if (getAuthToken()) {
        API.fetchCurrentUser()
          .then(u => {
            setCurrentUser(u);
            AppContext.resetUI();
            resolve(u);
          });
      } else {
        authenticate()
          .then(au => {

            if (!au) {
              resolve(undefined);
            } else {
              setAuthToken(au.token, au.authenticationType);
              setCurrentUser(au.userEntity);
              AppContext.resetUI();
              resolve(au.userEntity);
            }
          });
      }
    }, 500);
  });
}

export function logoutOtherTabs(user: UserEntity) {

  if (notifyLogout)
    localStorage.setItem('requestLogout' + Services.SessionSharing.getAppName(), user.userName + "&&" + new Date().toString());
}


export namespace Options {
  export let onLogout: () => void = () => {
    throw new Error("onLogout should be defined (check Main.tsx in Southwind)");
  }

  export let onLogin: (url?: string) => void = (url?: string) => {
    throw new Error("onLogin should be defined (check Main.tsx in Southwind)");
  }

  export let disableWindowsAuthentication: boolean;
  export let windowsAuthentication: boolean;
  export let userTicket: boolean;
  export let resetPassword: boolean;
}

export module API {
  export interface LoginRequest {
    userName: string;
    password: string;
    rememberMe?: boolean;
  }

  export interface LoginResponse {
    authenticationType: string;
    message?: string;
    token: string;
    userEntity: UserEntity;
  }

  export function login(loginRequest: LoginRequest): Promise<LoginResponse> {
    return ajaxPost({ url: "~/api/auth/login" }, loginRequest);
  }

  export function loginFromCookie(): Promise<LoginResponse | undefined> {
    return ajaxPost({ url: "~/api/auth/loginFromCookie", avoidAuthToken: true }, undefined);
  }

  export function loginWindowsAuthentication(throwError: boolean): Promise<LoginResponse | undefined> {
    return ajaxPost({ url: `~/api/auth/loginWindowsAuthentication?throwError=${throwError}`, avoidAuthToken: true }, undefined);
  }

  export function loginWithAzureAD(jwt: string): Promise<LoginResponse | undefined> {
    return ajaxPost({ url: "~/api/auth/loginWithAzureAD", avoidAuthToken: true }, jwt);
  }

  export function refreshToken(oldToken: string): Promise<LoginResponse | undefined> {
    return ajaxPost({ url: "~/api/auth/refreshToken", avoidAuthToken: true }, oldToken);
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
    return ajaxPost({ url: "~/api/auth/forgotPasswordEmail" }, request);
  }

  export function resetPassword(request: ResetPasswordRequest): Promise<LoginResponse> {
    return ajaxPost({ url: "~/api/auth/resetPassword" }, request);
  }

  export function changePassword(request: ChangePasswordRequest): Promise<LoginResponse> {
    return ajaxPost({ url: "~/api/auth/changePassword" }, request);
  }

  export function fetchCurrentUser(): Promise<UserEntity> {
    return ajaxGet({ url: "~/api/auth/currentUser", cache: "no-cache" });
  }

  export function logout(): Promise<void> {
    return ajaxPost({ url: "~/api/auth/logout" }, undefined);
  }
}
