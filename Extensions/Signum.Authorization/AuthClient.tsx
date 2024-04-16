import * as React from "react";
import { RouteObject, Location } from 'react-router'
import * as Services from '@framework/Services';
import { ImportComponent } from '@framework/ImportComponent'
import LoginPage from "./Login/LoginPage";
import * as AppContext from "@framework/AppContext";
import { ajaxGet, ajaxPost, ServiceError } from "@framework/Services";
import { is } from '@framework/Signum.Entities';
import { ifError } from "@framework/Globals";
import { Cookies } from "@framework/Cookies";
import { tryGetTypeInfo } from "@framework/Reflection";
import * as Reflection from "@framework/Reflection";
import { UserEntity, UserOperation} from './Signum.Authorization';
import { PermissionSymbol } from "@framework/Signum.Basics";
import { EntityOperationSettings, Operations } from "../../Signum/React/Operations";

export namespace AuthClient {
  
  export function startPublic(options: { routes: RouteObject[], userTicket: boolean, notifyLogout: boolean }) {
    Options.userTicket = options.userTicket;
  
    options.routes.push({ path: "/auth/login", element: <ImportComponent onImport={() => import("./Login/LoginPage")} /> });
    options.routes.push({ path: "/auth/changePassword", element: <ImportComponent onImport={() => import("./Login/ChangePasswordPage")} /> });
    options.routes.push({ path: "/auth/changePasswordSuccess", element: <ImportComponent onImport={() => import("./Login/ChangePasswordSuccessPage")} /> });

    Operations.addSettings(new EntityOperationSettings(UserOperation.AutoDeactivate, { hideOnCanExecute: true, isVisible: () => false }));

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
  
  
  export namespace Options {
    export let AuthHeader = "Authorization";
    export let disableWindowsAuthentication = false;
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
  
    if (Reflection.isStarted())
      throw new Error("call AuthClient.registerUserTicketAuthenticator in MainPublic.tsx before AuthClient.autoLogin");
  
    authenticators.push(loginFromCookie);
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
  
    export let onLogin: (back?: string) => void = () => {
      throw new Error("onLogin should be defined (check MainPublic.tsx in Southwind)");
    }
  
    export let userTicket: boolean;
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
  
  
    export interface ChangePasswordRequest {
      oldPassword: string;
      newPassword: string;
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
