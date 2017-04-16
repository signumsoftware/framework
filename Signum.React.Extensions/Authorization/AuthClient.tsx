import * as React from 'react'
import { Route } from 'react-router'
import { ModifiableEntity, EntityPack, is, OperationSymbol } from '../../../Framework/Signum.React/Scripts/Signum.Entities';
import { ifError } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet, ajaxGetRaw, saveFile, ServiceError } from '../../../Framework/Signum.React/Scripts/Services';
import * as Services from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import { tasks, LineBase, LineBaseProps } from '../../../Framework/Signum.React/Scripts/Lines/LineBase'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PropertyRouteEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import ButtonBar from '../../../Framework/Signum.React/Scripts/Frames/ButtonBar'
import { PseudoType, QueryKey, getTypeInfo, PropertyRouteType, OperationInfo, isQueryDefined, getQueryInfo, GraphExplorer } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { UserEntity, RoleEntity, UserOperation, PermissionSymbol, PropertyAllowed, TypeAllowedBasic, AuthAdminMessage, BasicPermission } from './Signum.Entities.Authorization'
import { PermissionRulePack, TypeRulePack, OperationRulePack, PropertyRulePack, QueryRulePack} from './Signum.Entities.Authorization'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import Login from './Login/Login';
import { LoadRoute } from "../../../Framework/Signum.React/Scripts/LoadComponent";

export let userTicket: boolean;
export let resetPassword: boolean;


Services.AuthTokenFilter.addAuthToken = addAuthToken;

export function startPublic(options: { routes: JSX.Element[], userTicket: boolean, resetPassword: boolean }) {
    userTicket = options.userTicket;
    resetPassword = options.resetPassword;

    options.routes.push(<Route path="auth">
        <LoadRoute path="login" onLoadModule={() => _import("./Login/Login")} />
        <LoadRoute path="changePassword" onLoadModule={() => _import("./Login/ChangePassword")} />
    </Route>);
}

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

    Navigator.addSettings(new EntitySettings(UserEntity, e => _import('./Templates/User')));
    Navigator.addSettings(new EntitySettings(RoleEntity, e => _import('./Templates/Role')));
    Operations.addSettings(new EntityOperationSettings(UserOperation.SetPassword, { isVisible: ctx => false }));

    if (options.properties) {
        tasks.push(taskAuthorizeProperties);
        GraphExplorer.TypesLazilyCreated.push(PropertyRouteEntity.typeName);
        Navigator.addSettings(new EntitySettings(PropertyRulePack, e => _import('./Admin/PropertyRulePackControl')));
    }

    if (options.types) {
        Navigator.isCreableEvent.push(navigatorIsCreable);
        Navigator.isReadonlyEvent.push(navigatorIsReadOnly);
        Navigator.isViewableEvent.push(navigatorIsViewable);

        Navigator.addSettings(new EntitySettings(TypeRulePack, e => _import('./Admin/TypeRulePackControl')));

        QuickLinks.registerQuickLink(RoleEntity, ctx => new QuickLinks.QuickLinkAction("types", AuthAdminMessage.TypeRules.niceToString(),
            e => Api.fetchTypeRulePack(ctx.lite.id!).then(pack => Navigator.navigate(pack)).done(),
            { isVisible: isPermissionAuthorized(BasicPermission.AdminRules) }));
    }

    if (options.operations) {
        Operations.isOperationAllowedEvent.push(isOperationAuthorized);

        Navigator.addSettings(new EntitySettings(OperationRulePack, e => _import('./Admin/OperationRulePackControl')));
    }

    if (options.queries) {
        Finder.isFindableEvent.push(queryIsFindable);

        Navigator.addSettings(new EntitySettings(QueryRulePack, e => _import('./Admin/QueryRulePackControl')));
    }

    if (options.permissions) {

        Navigator.addSettings(new EntitySettings(PermissionRulePack, e => _import('./Admin/PermissionRulePackControl')));

        QuickLinks.registerQuickLink(RoleEntity, ctx => new QuickLinks.QuickLinkAction("permissions", AuthAdminMessage.PermissionRules.niceToString(),
            e => Api.fetchPermissionRulePack(ctx.lite.id!).then(pack => Navigator.navigate(pack)).done(),
            { isVisible: isPermissionAuthorized(BasicPermission.AdminRules) }));
    }

    OmniboxClient.registerSpecialAction({
        allowed: () => isPermissionAuthorized(BasicPermission.AdminRules),
        key: "DownloadAuthRules",
        onClick: () => { Api.downloadAuthRules(); return Promise.resolve(undefined); }
    });
}

export function queryIsFindable(queryKey: string) {
    return getQueryInfo(queryKey).queryAllowed;
}

function isOperationAuthorized(operation: OperationInfo | OperationSymbol | string): boolean {
    var key = (operation as OperationInfo | OperationSymbol).key || operation as string;
    const member = getTypeInfo(key.before(".")).members[key.after(".")];
    if (member == null)
        throw new Error(`Operation ${key} not found, consider Synchronize`);

    return  member.operationAllowed;
}

export function taskAuthorizeProperties(lineBase: LineBase<LineBaseProps, LineBaseProps>, state: LineBaseProps) {
    if (state.ctx.propertyRoute &&
        state.ctx.propertyRoute.propertyRouteType == "Field") {

        const member = state.ctx.propertyRoute.member;

        switch ((member as any).propertyAllowed as PropertyAllowed) {
            case "None":
                state.visible = false;
                break;
            case "Read":
                state.readOnly = true;
                break;
            case "Modify":
                break;
        }
    }
}

export function navigatorIsReadOnly(typeName: string, entityPack?: EntityPack<ModifiableEntity>) {
    const ti = getTypeInfo(typeName);

    if (ti == undefined)
        return false;

    if (entityPack && entityPack.typeAllowed)
        return entityPack.typeAllowed == "None" || entityPack.typeAllowed == "Read";

    return ti.typeAllowed == "None" || ti.typeAllowed == "Read";
}

export function navigatorIsViewable(typeName: string, entityPack?: EntityPack<ModifiableEntity>) {
    const ti = getTypeInfo(typeName);

    if (ti == undefined)
        return true;

    if (entityPack && entityPack.typeAllowed)
        return entityPack.typeAllowed != "None";

    return ti.typeAllowed != "None";
}

export function navigatorIsCreable(typeName: string) {
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
        .catch(ifError(ServiceError, e => {
            if (e.status == 426 && e.httpError.ExceptionType.endsWith(".NewTokenRequiredException")) {

                if (token != getAuthToken())
                    return makeCall();

                return Api.refreshToken(token).then(resp => {
                    setAuthToken(resp.token);
                    setCurrentUser(resp.userEntity)
                    
                    options.headers!["Authorization"] = "Bearer " + resp.token;

                    return makeCall();
                }, e2 => {
                    setAuthToken(undefined);
                    Navigator.currentHistory.push("~/auth/login");
                    throw e;
                });
            }

            if (e.httpError.ExceptionType && e.httpError.ExceptionType.endsWith(".AuthenticationException")) {
                setAuthToken(undefined);
                Navigator.currentHistory.push("~/auth/login");
            }

            throw e;
        }));
}

export function getAuthToken(): string | undefined {
    return sessionStorage.getItem("authToken") || undefined;
}

export function setAuthToken(authToken: string | undefined): void{
    sessionStorage.setItem("authToken", authToken || "");
}

export function autoLogin(): Promise<UserEntity> {

    if (Navigator.currentUser)
        return Promise.resolve(Navigator.currentUser);

    if (getAuthToken())
        return Api.fetchCurrentUser().then(u => {
            setCurrentUser(u);
            Navigator.resetUI();
            return u;
        });

    return new Promise<UserEntity>((resolve) => {
        setTimeout(() => {
            if (getAuthToken()) {
                Api.fetchCurrentUser()
                    .then(u => {
                        setCurrentUser(u);
                        Navigator.resetUI();
                        resolve(u);
                    });
            } else {
                Api.loginFromCookie()
                    .then(respo => {

                        if (!respo) {
                            resolve(undefined);
                        } else {
                            setAuthToken(respo.token);
                            setCurrentUser(respo.userEntity);
                            Navigator.resetUI();
                            resolve(respo.userEntity);
                        }
                    });
            }
        }, 500);
    });
}

export function logout() {

    Api.logout().then(() => {
        setAuthToken(undefined);
        setCurrentUser(undefined);
        Options.onLogout();
    }).done();
}

export namespace Options {
    export let onLogout = () => {
        Navigator.currentHistory.push("~/");
    }

    export let onLogin = (url?: string) => {
        Navigator.currentHistory.push(url || "~/");
    }
}

export function isPermissionAuthorized(permission: PermissionSymbol | string) {
    var key = (permission as PermissionSymbol).key || permission as string;
    const member = getTypeInfo(key.before(".")).members[key.after(".")];
    return member.permissionAllowed;
}

export function asserPermissionAuthorized(permission: PermissionSymbol | string) {
    var key = (permission as PermissionSymbol).key || permission as string;
    if (!isPermissionAuthorized(key))
        throw new Error(`Permission ${key} is denied`);
}

export module Api {

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

    export function changePassword(request: ChangePasswordRequest): Promise<UserEntity> {
        return ajaxPost<UserEntity>({ url: "~/api/auth/ChangePassword" }, request);
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
        return ajaxPost<void>({ url: "~/api/authAdmin/permissionRules"}, rules);
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

declare module '../../../Framework/Signum.React/Scripts/Reflection' {

    export interface TypeInfo {
        typeAllowed: TypeAllowedBasic;
        queryAllowed: boolean;
    }

    export interface MemberInfo {
        propertyAllowed: PropertyAllowed;
        queryAllowed: boolean;
        operationAllowed: boolean;
        permissionAllowed: boolean;
    }
}

declare module '../../../Framework/Signum.React/Scripts/Signum.Entities' {

    export interface EntityPack<T extends ModifiableEntity> {
        typeAllowed?: TypeAllowedBasic;
    }
}


