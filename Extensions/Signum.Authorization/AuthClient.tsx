import * as React from "react";
import { RouteObject, Location } from 'react-router'
import * as Services from '@framework/Services';
import { ImportComponent } from '@framework/ImportComponent'
import * as AppContext from "@framework/AppContext";
import { ajaxGet, ajaxPost, ServiceError } from "@framework/Services";
import { is } from '@framework/Signum.Entities';
import { ifError } from "@framework/Globals";
import { Cookies } from "@framework/Cookies";
import { tryGetTypeInfo } from "@framework/Reflection";
import * as Reflection from "@framework/Reflection";
import { LoginAuthMessage, UserEntity, UserOperation} from './Signum.Authorization';
import { PermissionSymbol } from "@framework/Signum.Basics";
import { EntityOperationSettings, Operations } from "../../Signum/React/Operations";

export namespace AuthClient {
  
  let pendingPasswordChangeUser: UserEntity | undefined;
  export function getPendingPasswordChangeUser(): UserEntity | undefined { return pendingPasswordChangeUser; }
  export function setPendingPasswordChangeUser(v: UserEntity | undefined): void { pendingPasswordChangeUser = v; }

  export interface PasswordValidationResult { message: string, level: "error" | "warning" }

  export function startPublic(options: { routes: RouteObject[], userTicket: boolean, notifyLogout: boolean }): void {
    Options.userTicket = options.userTicket;
  
    options.routes.push({ path: "/auth/login", element: <ImportComponent onImport={() => import("./Login/LoginPage")} /> });
    options.routes.push({ path: "/auth/changePassword", element: <ImportComponent onImport={() => import("./Login/ChangePasswordPage")} /> });
    options.routes.push({ path: "/auth/changePasswordSuccess", element: <ImportComponent onImport={() => import("./Login/ChangePasswordSuccessPage")} /> });

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
  
  
  export const Options = {
    AuthHeader: "Authorization",
    disableWindowsAuthentication: false,
    validatePassword: undefined as ((password: string, user: UserEntity) => Promise<PasswordValidationResult | null>) | undefined,
    getCookie(): string | null { return Cookies.get("sfUser"); },
    removeCookie(): void { return Cookies.remove("sfUser", "/", document.location.hostname); },
    onLogout: (): Promise<void> => {
      throw new Error("onLogout should be defined (check MainPublic.tsx in Southwind)");
    },
    onLogin: (_back?: string): void => {
      throw new Error("onLogin should be defined (check MainPublic.tsx in Southwind)");
    },
    userTicket: false,
  };

  export const LoginOptions = {
    customLoginButtons: null as ((ctx: LoginContext) => React.ReactNode) | null,
    showLoginForm: "yes" as "yes" | "no" | "initially_not",
    usernameLabel: (): string => LoginAuthMessage.Username.niceToString(),
    resetPasswordControl: () => null as null | React.ReactElement,
  };

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
  
  
  
  export async function authenticate(): Promise<AuthenticatedUser | undefined> {

    for (let i = 0; i < authenticators.length; i++) {

      var f = authenticators[i];
      let aUser = await f();
      console.log(f.name + "..." + (aUser ? "OK" : "Null"));
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
  
  export function logout(): void {
    var user = currentUser();
    if (user == null)
      return;
  
    Options.removeCookie();
  
    API.logout().then(() => {
      logoutInternal();
      logoutOtherTabs(user);
    });
  }
  
  async function logoutInternal() : Promise<void> {
    setAuthToken(undefined, undefined);
    setCurrentUser(undefined);
    Options.disableWindowsAuthentication = true;
    await Options.onLogout();
  }
  
  Services.AuthTokenFilter.Options.addAuthToken = addAuthToken;
  
  export const onCurrentUserChanged: Array<(newUser: UserEntity | undefined, avoidReRender?: boolean) => void> = [];
  
  export function setCurrentUser(user: UserEntity | undefined, avoidReRender?: boolean): void {
  
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
    console.log("setAuthToken(" + (authToken == null ? "null" : authToken.etc(10)) + ", " + authenticationType + ")");
    sessionStorage.setItem("authToken", authToken ?? "");
    sessionStorage.setItem("authenticationType", authenticationType ?? "");
  }
  
  export function registerUserTicketAuthenticator(): void {
  
    if (Reflection.isStarted())
      throw new Error("call AuthClient.registerUserTicketAuthenticator in MainPublic.tsx before AuthClient.autoLogin");
  
    authenticators.push(loginFromCookie);
  }
  
  export function autoLogin(): Promise<UserEntity | undefined> {
    if (AppContext.currentUser)
      return Promise.resolve(AppContext.currentUser as UserEntity);

    function loginWithAuthToken() {
      return API.fetchCurrentUser().then(u => {
        if (u.mustChangePassword) {
          console.log("loginWithAuthToken...OK (mustChangePassword)");
          pendingPasswordChangeUser = u;
          return undefined;
        } else {
          console.log("loginWithAuthToken...OK");
          setCurrentUser(u);
          AppContext.resetUI();
          return u;
        }
      }, e => {
        console.log("loginWithAuthToken...Error");
        console.error(e);
        setAuthToken(undefined, undefined);
        return undefined;
      });
    }

    if (getAuthToken())
      return loginWithAuthToken();
  
    return new Promise<undefined>((resolve) => window.setTimeout(() => resolve(undefined), 500))
      .then(() => {
        if (getAuthToken()) {
          return loginWithAuthToken();
        } else {
          return authenticate()
            .then(au => {
              if (!au) {
                return undefined;
              } else {
                setAuthToken(au.token, au.authenticationType);
                if (au.userEntity.mustChangePassword) {
                  pendingPasswordChangeUser = au.userEntity;
                  if (AppContext._internalRouter) {
                    AppContext.navigate("/auth/changePassword");
                  }
                  return undefined;
                } else {
                  setCurrentUser(au.userEntity);
                  AppContext.resetUI();
                  return au.userEntity;
                }
              }
            });
        }
      });
  }

  export interface LoginContext {
    loading: string | undefined;
    setLoading: (loading: string | undefined) => void;
    userNameRef?: React.RefObject<HTMLInputElement | null>;
  }


  export function logoutOtherTabs(user: UserEntity): void {
    if (notifyLogout)
      localStorage.setItem('requestLogout' + Services.SessionSharing.getAppName(), user.userName + "&&" + new Date().toString());
  }
  
  
  export type AuthenticationType = "database" | "resetPassword" | "changePassword" | "api-key" | "azureAD" | "cookie" | "windows";
  
  export namespace API {
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
  
  
    export interface ChangePasswordRequest {
      oldPassword: string;
      newPassword: string;
    }
  
  
    export function relogin(): Promise<LoginResponse> {
      return ajaxGet({ url: "/api/auth/relogin" });
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
}
