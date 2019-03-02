import * as React from 'react'
import { ModifiableEntity, EntityPack, is } from '@framework/Signum.Entities';
import { ifError } from '@framework/Globals';
import { ajaxPost, ajaxGet, ajaxGetRaw, saveFile, ServiceError } from '@framework/Services';
import * as Services from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import { tasks, LineBase, LineBaseProps } from '@framework/Lines/LineBase'
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

export let userTicket: boolean;
export let resetPassword: boolean;


Services.AuthTokenFilter.addAuthToken = addAuthToken;

export function registerUserTicketAuthenticator() {
  authenticators.push(loginFromCookie);
}

export function startPublic(options: { routes: JSX.Element[], userTicket: boolean, resetPassword: boolean, notifyLogout: boolean }) {
  userTicket = options.userTicket;
  resetPassword = options.resetPassword;

  if (userTicket) {
    if (!authenticators.contains(loginFromCookie))
      throw new Error("call AuthClient.registerUserTicketAuthenticator in Main.tsx before AuthClient.autoLogin");
  }

  options.routes.push(<ImportRoute path="~/auth/login" onImportModule={() => import("./Login/Login")} />);
  options.routes.push(<ImportRoute path="~/auth/changePassword" onImportModule={() => import("./Login/ChangePassword")} />);


  if (options.notifyLogout) {
    notifyLogout = options.notifyLogout;

    window.addEventListener("storage", se => {
      if (se.key == 'requestLogout' + Services.SessionSharing.getAppName()) {

        var userName = se.newValue!.before("&&");

        var cu = currentUser();
        if (cu && cu.userName == userName)
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

    Navigator.addSettings(new EntitySettings(TypeRulePack, e => import('./Admin/TypeRulePackControl')));

    QuickLinks.registerQuickLink(RoleEntity, ctx => new QuickLinks.QuickLinkAction("types", AuthAdminMessage.TypeRules.niceToString(),
      e => API.fetchTypeRulePack(ctx.lite.id!).then(pack => Navigator.navigate(pack)).done(),
      { isVisible: isPermissionAuthorized(BasicPermission.AdminRules) }));
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
      { isVisible: isPermissionAuthorized(BasicPermission.AdminRules) }));
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
    return this.member != null && this.member.propertyAllowed == "Modify"
  }
}

export function queryIsFindable(queryKey: string, fullScreen: boolean) {
  var allowed = getQueryInfo(queryKey).queryAllowed;

  return allowed == "Allow" || allowed == "EmbeddedOnly" && !fullScreen;
}


export function isOperationInfoAllowed(oi: OperationInfo) {
  return oi.operationAllowed;
}

export function taskAuthorizeProperties(lineBase: LineBase<LineBaseProps, LineBaseProps>, state: LineBaseProps) {
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
      case "Modify":
        break;
    }
  }
}

export function navigatorIsReadOnly(typeName: PseudoType, entityPack?: EntityPack<ModifiableEntity>) {
  const ti = getTypeInfo(typeName);

  if (ti == undefined)
    return false;

  if (entityPack && entityPack.typeAllowed)
    return entityPack.typeAllowed == "None" || entityPack.typeAllowed == "Read";

  return ti.typeAllowed == "None" || ti.typeAllowed == "Read";
}

export function navigatorIsViewable(typeName: PseudoType, entityPack?: EntityPack<ModifiableEntity>) {
  const ti = getTypeInfo(typeName);

  if (ti == undefined)
    return true;

  if (entityPack && entityPack.typeAllowed)
    return entityPack.typeAllowed != "None";

  return ti.typeAllowed != "None";
}

export function navigatorIsCreable(typeName: PseudoType) {
  const ti = getTypeInfo(typeName);

  return ti == undefined || ti.typeAllowed == "Create";
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

  options.headers["Authorization"] = "Bearer " + token;

  return makeCall()
    .then(r => {
      var newToken = r.headers.get("New_Token");
      if (newToken) {
        setAuthToken(newToken);
        API.fetchCurrentUser()
          .then(cu => setCurrentUser(cu))
          .done();
      }

      return r;

    }, ifError<ServiceError, Response>(ServiceError, e => {

      if (e.httpError.exceptionType && e.httpError.exceptionType.endsWith(".AuthenticationException")) {
        setAuthToken(undefined);
        Navigator.history.push("~/auth/login");
      }

      throw e;
    }));
}

export function getAuthToken(): string | undefined {
  return sessionStorage.getItem("authToken") || undefined;
}

export function setAuthToken(authToken: string | undefined): void {
  sessionStorage.setItem("authToken", authToken || "");
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
          .then(authenticatedUser => {

            if (!authenticatedUser) {
              resolve(undefined);
            } else {
              setAuthToken(authenticatedUser.token);
              setCurrentUser(authenticatedUser.userEntity);
              Navigator.resetUI();
              resolve(authenticatedUser.userEntity);
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
  setAuthToken(undefined);
  setCurrentUser(undefined);

  Options.onLogout();
}

export namespace Options {
  export let onLogout: () => void = () => {
    throw new Error("onLogout should be defined (check Main.tsx in Southwind)");
  }

  export let onLogin: (url?: string) => void = (url?: string) => {
    throw new Error("onLogin should be defined (check Main.tsx in Southwind)");
  }
}

export function isPermissionAuthorized(permission: PermissionSymbol | string) {
  var key = (permission as PermissionSymbol).key || permission as string;
  const type = getTypeInfo(key.before("."));
  if (!type)
    throw new Error(`Type '${key.before(".")}' not found. Consider adding PermissionAuthLogic.RegisterPermissions(${key}) and Synchronize`);

  const member = type.members[key.after(".")];
  if (!member)
    throw new Error(`Member '${key.after(".")}' not found. Consider adding PermissionAuthLogic.RegisterPermissions(${key}) and Synchronize`);

  return member.permissionAllowed;
}

export function asserPermissionAuthorized(permission: PermissionSymbol | string) {
  var key = (permission as PermissionSymbol).key || permission as string;
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
    message: string;
    userEntity: UserEntity;
    token: string;
  }

  export function login(loginRequest: LoginRequest): Promise<LoginResponse> {
    return ajaxPost<LoginResponse>({ url: "~/api/auth/login" }, loginRequest);
  }

  export function loginFromCookie(): Promise<LoginResponse> {
    return ajaxPost<LoginResponse>({ url: "~/api/auth/loginFromCookie", avoidAuthToken: true }, undefined);
  }


  export function refreshToken(oldToken: string): Promise<LoginResponse> {
    return ajaxPost<LoginResponse>({ url: "~/api/auth/refreshToken", avoidAuthToken: true }, oldToken);
  }

  export interface ChangePasswordRequest {
    oldPassword: string;
    newPassword: string;
  }

  export function changePassword(request: ChangePasswordRequest): Promise<LoginResponse> {
    return ajaxPost<LoginResponse>({ url: "~/api/auth/ChangePassword" }, request);
  }

  export function fetchCurrentUser(): Promise<UserEntity> {
    return ajaxGet<UserEntity>({ url: "~/api/auth/currentUser", cache: "no-cache" });
  }

  export function logout(): Promise<void> {
    return ajaxPost<void>({ url: "~/api/auth/logout" }, undefined);
  }

  export function fetchPermissionRulePack(roleId: number | string): Promise<PermissionRulePack> {
    return ajaxGet<PermissionRulePack>({ url: "~/api/authAdmin/permissionRules/" + roleId, cache: "no-cache" });
  }

  export function savePermissionRulePack(rules: PermissionRulePack): Promise<void> {
    return ajaxPost<void>({ url: "~/api/authAdmin/permissionRules" }, rules);
  }


  export function fetchTypeRulePack(roleId: number | string): Promise<TypeRulePack> {
    return ajaxGet<TypeRulePack>({ url: "~/api/authAdmin/typeRules/" + roleId, cache: "no-cache" });
  }

  export function saveTypeRulePack(rules: TypeRulePack): Promise<void> {
    return ajaxPost<void>({ url: "~/api/authAdmin/typeRules" }, rules);
  }


  export function fetchPropertyRulePack(typeName: string, roleId: number | string): Promise<PropertyRulePack> {
    return ajaxGet<PropertyRulePack>({ url: "~/api/authAdmin/propertyRules/" + typeName + "/" + roleId, cache: "no-cache" });
  }

  export function savePropertyRulePack(rules: PropertyRulePack): Promise<void> {
    return ajaxPost<void>({ url: "~/api/authAdmin/propertyRules" }, rules);
  }



  export function fetchOperationRulePack(typeName: string, roleId: number | string): Promise<OperationRulePack> {
    return ajaxGet<OperationRulePack>({ url: "~/api/authAdmin/operationRules/" + typeName + "/" + roleId, cache: "no-cache" });
  }

  export function saveOperationRulePack(rules: OperationRulePack): Promise<void> {
    return ajaxPost<void>({ url: "~/api/authAdmin/operationRules" }, rules);
  }



  export function fetchQueryRulePack(typeName: string, roleId: number | string): Promise<QueryRulePack> {
    return ajaxGet<QueryRulePack>({ url: "~/api/authAdmin/queryRules/" + typeName + "/" + roleId, cache: "no-cache" });
  }

  export function saveQueryRulePack(rules: QueryRulePack): Promise<void> {
    return ajaxPost<void>({ url: "~/api/authAdmin/queryRules" }, rules);
  }



  export function downloadAuthRules(): void {
    ajaxGetRaw({ url: "~/api/authAdmin/downloadAuthRules" })
      .then(response => saveFile(response))
      .done();
  }

}

declare module '@framework/Reflection' {

  export interface TypeInfo {
    typeAllowed: TypeAllowedBasic;
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


