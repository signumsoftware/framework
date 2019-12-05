import * as React from 'react'
import { ModifiableEntity, EntityPack, is, OperationSymbol } from '@framework/Signum.Entities';
import { ifError } from '@framework/Globals';
import { ajaxPost, ajaxGet, ajaxGetRaw, saveFile, ServiceError } from '@framework/Services';
import * as Services from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import { tasks, LineBaseProps, LineBaseController } from '@framework/Lines/LineBase'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import * as QuickLinks from '@framework/QuickLinks'
import { EntityOperationSettings } from '@framework/Operations'
import { PropertyRouteEntity } from '@framework/Signum.Entities.Basics'
import { PseudoType, getTypeInfo, OperationInfo, getQueryInfo, GraphExplorer, PropertyRoute } from '@framework/Reflection'
import * as Operations from '@framework/Operations'
import { UserEntity, RoleEntity, UserOperation, PermissionSymbol, PropertyAllowed, TypeAllowedBasic, AuthAdminMessage, BasicPermission } from './Signum.Entities.Authorization'
import { PermissionRulePack, TypeRulePack, OperationRulePack, PropertyRulePack, QueryRulePack, QueryAllowed } from './Signum.Entities.Authorization'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import { ImportRoute } from "@framework/AsyncImport";
import Login, { LoginWithWindowsButton } from './Login/Login';

Services.AuthTokenFilter.addAuthToken = addAuthToken;

export function registerUserTicketAuthenticator() {
  authenticators.push(loginFromCookie);
}

/* Install and enable Windows authentication in IIS https://docs.microsoft.com/en-us/aspnet/core/security/authentication/windowsauth?view=aspnetcore-2.2&tabs=visual-studio */
export function registerWindowsAuthenticator() {
  authenticators.push(loginWindowsAuthentication);
}

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

export function logoutOtherTabs(user: UserEntity) {

  if (notifyLogout)
    localStorage.setItem('requestLogout' + Services.SessionSharing.getAppName(), user.userName + "&&" + new Date().toString());
}

var notifyLogout: boolean;

export let types: boolean;
export let properties: boolean;
export let operations: boolean;
export let queries: boolean;
export let permissions: boolean;

export function start(options: { routes: JSX.Element[], types: boolean; properties: boolean, operations: boolean, queries: boolean; permissions: boolean }) {

  types = options.types;
  properties = options.properties;
  operations = options.operations;
  queries = options.queries;
  permissions = options.permissions;

  Navigator.addSettings(new EntitySettings(UserEntity, e => import('./Templates/User')));
  Navigator.addSettings(new EntitySettings(RoleEntity, e => import('./Templates/Role')));
  Operations.addSettings(new EntityOperationSettings(UserOperation.SetPassword, { isVisible: ctx => false }));

  if (options.properties) {
    tasks.push(taskAuthorizeProperties);
    GraphExplorer.TypesLazilyCreated.push(PropertyRouteEntity.typeName);
    Navigator.addSettings(new EntitySettings(PropertyRulePack, e => import('./Admin/PropertyRulePackControl')));
  }

  if (options.types) {
    Navigator.isCreableEvent.push(navigatorIsCreable);
    Navigator.isReadonlyEvent.push(navigatorIsReadOnly);
    Navigator.isViewableEvent.push(navigatorIsViewable);
    Operations.Options.maybeReadonly = ti => ti.maxTypeAllowed == "Write" && ti.minTypeAllowed != "Write";
    Navigator.addSettings(new EntitySettings(TypeRulePack, e => import('./Admin/TypeRulePackControl')));

    QuickLinks.registerQuickLink(RoleEntity, ctx => new QuickLinks.QuickLinkAction("types", AuthAdminMessage.TypeRules.niceToString(),
      e => API.fetchTypeRulePack(ctx.lite.id!).then(pack => Navigator.navigate(pack)).done(),
      { isVisible: isPermissionAuthorized(BasicPermission.AdminRules), icon: "shield-alt", iconColor: "red" }));
  }

  if (options.operations) {
    Operations.isOperationInfoAllowedEvent.push(isOperationInfoAllowed);

    Navigator.addSettings(new EntitySettings(OperationRulePack, e => import('./Admin/OperationRulePackControl')));
  }

  if (options.queries) {
    Finder.isFindableEvent.push(queryIsFindable);

    Navigator.addSettings(new EntitySettings(QueryRulePack, e => import('./Admin/QueryRulePackControl')));
  }

  if (options.permissions) {

    Navigator.addSettings(new EntitySettings(PermissionRulePack, e => import('./Admin/PermissionRulePackControl')));

    QuickLinks.registerQuickLink(RoleEntity, ctx => new QuickLinks.QuickLinkAction("permissions", AuthAdminMessage.PermissionRules.niceToString(),
      e => API.fetchPermissionRulePack(ctx.lite.id!).then(pack => Navigator.navigate(pack)).done(),
      { isVisible: isPermissionAuthorized(BasicPermission.AdminRules), icon: "shield-alt", iconColor: "orange" }));
  }

  OmniboxClient.registerSpecialAction({
    allowed: () => isPermissionAuthorized(BasicPermission.AdminRules),
    key: "DownloadAuthRules",
    onClick: () => { API.downloadAuthRules(); return Promise.resolve(undefined); }
  });

  PropertyRoute.prototype.canRead = function () {
    return this.member != null && this.member.propertyAllowed != "None"
  }

  PropertyRoute.prototype.canModify = function () {
    return this.member != null && this.member.propertyAllowed == "Write"
  }
}

export function queryIsFindable(queryKey: string, fullScreen: boolean) {
  var allowed = getQueryInfo(queryKey).queryAllowed;

  return allowed == "Allow" || allowed == "EmbeddedOnly" && !fullScreen;
}


export function isOperationInfoAllowed(oi: OperationInfo) {
  return oi.operationAllowed;
}

export function isOperationAllowed(type: PseudoType, operation: OperationSymbol) {
  var ti = getTypeInfo(type);
  return isOperationInfoAllowed(ti.operations![operation.key]);
}

export function taskAuthorizeProperties(lineBase: LineBaseController<LineBaseProps>, state: LineBaseProps) {
  if (state.ctx.propertyRoute &&
    state.ctx.propertyRoute.propertyRouteType == "Field") {

    const member = state.ctx.propertyRoute.member;

    switch (member!.propertyAllowed) {
      case "None":
        state.visible = false;
        break;
      case "Read":
        state.ctx.readOnly = true;
        break;
      case "Write":
        break;
    }
  }
}

export function navigatorIsReadOnly(typeName: PseudoType, entityPack?: EntityPack<ModifiableEntity>) {
  const ti = getTypeInfo(typeName);

  if (ti == undefined)
    return false;

  if (entityPack?.typeAllowed)
    return entityPack.typeAllowed == "None" || entityPack.typeAllowed == "Read";

  return ti.maxTypeAllowed == "None" || ti.maxTypeAllowed == "Read";
}

export function navigatorIsViewable(typeName: PseudoType, entityPack?: EntityPack<ModifiableEntity>) {
  const ti = getTypeInfo(typeName);

  if (ti == undefined)
    return true;

  if (entityPack?.typeAllowed)
    return entityPack.typeAllowed != "None";

  return ti.maxTypeAllowed != "None";
}

export function navigatorIsCreable(typeName: PseudoType) {
  const ti = getTypeInfo(typeName);

  return ti == undefined || ti.maxTypeAllowed == "Write";
}

export function currentUser(): UserEntity {
  return Navigator.currentUser as UserEntity;
}

export const onCurrentUserChanged: Array<(newUser: UserEntity | undefined, avoidReRender?: boolean) => void> = [];

export function setCurrentUser(user: UserEntity | undefined, avoidReRender?: boolean) {

  const changed = !is(Navigator.currentUser, user, true);

  Navigator.setCurrentUser(user);

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
        setAuthToken(newToken, getAuthorizationType());
        API.fetchCurrentUser()
          .then(cu => setCurrentUser(cu))
          .done();
      }

      return r;

    }, ifError<ServiceError, Response>(ServiceError, e => {

      if (e.httpError.exceptionType?.endsWith(".AuthenticationException")) {
        setAuthToken(undefined, undefined);
        Navigator.history.push("~/auth/login");
      }

      throw e;
    }));
}

export function getAuthToken(): string | undefined {
  return sessionStorage.getItem("authToken") ?? undefined;
}

export function getAuthorizationType(): string | undefined {
  return sessionStorage.getItem("authorizationType") ?? undefined;
}

export function setAuthToken(authToken: string | undefined, authorizationType: string | undefined): void {
  sessionStorage.setItem("authToken", authToken ?? "");
  sessionStorage.setItem("authorizationType", authorizationType ?? "");
}

export function autoLogin(): Promise<UserEntity | undefined> {
  if (Navigator.currentUser)
    return Promise.resolve(Navigator.currentUser as UserEntity);

  if (getAuthToken())
    return API.fetchCurrentUser().then(u => {
      setCurrentUser(u);
      Navigator.resetUI();
      return u;
    });

  return new Promise<UserEntity>((resolve) => {
    setTimeout(() => {
      if (getAuthToken()) {
        API.fetchCurrentUser()
          .then(u => {
            setCurrentUser(u);
            Navigator.resetUI();
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
              Navigator.resetUI();
              resolve(au.userEntity);
            }
          });
      }
    }, 500);
  });
}

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

export function isPermissionAuthorized(permission: PermissionSymbol | string) {
  var key = (permission as PermissionSymbol).key ?? permission as string;
  const type = getTypeInfo(key.before("."));
  if (!type)
    throw new Error(`Type '${key.before(".")}' not found. Consider adding PermissionAuthLogic.RegisterPermissions(${key}) and Synchronize`);

  const member = type.members[key.after(".")];
  if (!member)
    throw new Error(`Member '${key.after(".")}' not found. Consider adding PermissionAuthLogic.RegisterPermissions(${key}) and Synchronize`);

  return member.permissionAllowed;
}

export function assertPermissionAuthorized(permission: PermissionSymbol | string) {
  var key = (permission as PermissionSymbol).key ?? permission as string;
  if (!isPermissionAuthorized(key))
    throw new Error(`Permission ${key} is denied`);
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

  export function refreshToken(oldToken: string): Promise<LoginResponse| undefined> {
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
    code: string ;
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

  export function fetchPermissionRulePack(roleId: number | string): Promise<PermissionRulePack> {
    return ajaxGet({ url: "~/api/authAdmin/permissionRules/" + roleId, cache: "no-cache" });
  }

  export function savePermissionRulePack(rules: PermissionRulePack): Promise<void> {
    return ajaxPost({ url: "~/api/authAdmin/permissionRules" }, rules);
  }


  export function fetchTypeRulePack(roleId: number | string): Promise<TypeRulePack> {
    return ajaxGet({ url: "~/api/authAdmin/typeRules/" + roleId, cache: "no-cache" });
  }

  export function saveTypeRulePack(rules: TypeRulePack): Promise<void> {
    return ajaxPost({ url: "~/api/authAdmin/typeRules" }, rules);
  }


  export function fetchPropertyRulePack(typeName: string, roleId: number | string): Promise<PropertyRulePack> {
    return ajaxGet({ url: "~/api/authAdmin/propertyRules/" + typeName + "/" + roleId, cache: "no-cache" });
  }

  export function savePropertyRulePack(rules: PropertyRulePack): Promise<void> {
    return ajaxPost({ url: "~/api/authAdmin/propertyRules" }, rules);
  }



  export function fetchOperationRulePack(typeName: string, roleId: number | string): Promise<OperationRulePack> {
    return ajaxGet({ url: "~/api/authAdmin/operationRules/" + typeName + "/" + roleId, cache: "no-cache" });
  }

  export function saveOperationRulePack(rules: OperationRulePack): Promise<void> {
    return ajaxPost({ url: "~/api/authAdmin/operationRules" }, rules);
  }



  export function fetchQueryRulePack(typeName: string, roleId: number | string): Promise<QueryRulePack> {
    return ajaxGet({ url: "~/api/authAdmin/queryRules/" + typeName + "/" + roleId, cache: "no-cache" });
  }

  export function saveQueryRulePack(rules: QueryRulePack): Promise<void> {
    return ajaxPost({ url: "~/api/authAdmin/queryRules" }, rules);
  }



  export function downloadAuthRules(): void {
    ajaxGetRaw({ url: "~/api/authAdmin/downloadAuthRules" })
      .then(response => saveFile(response))
      .done();
  }

}

declare module '@framework/Reflection' {

  export interface TypeInfo {
    minTypeAllowed: TypeAllowedBasic;
    maxTypeAllowed: TypeAllowedBasic;
    queryAllowed: QueryAllowed;
  }

  export interface MemberInfo {
    propertyAllowed: PropertyAllowed;
    queryAllowed: QueryAllowed;
    permissionAllowed: boolean;
  }

  export interface OperationInfo {
    operationAllowed: boolean;
  }

  export interface PropertyRoute {
    canRead(): boolean;
    canModify(): boolean;
  }
}

declare module '@framework/Signum.Entities' {

  export interface EntityPack<T extends ModifiableEntity> {
    typeAllowed?: TypeAllowedBasic;
  }
}


