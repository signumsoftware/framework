import * as React from 'react'
import { Route } from 'react-router'
import { ModifiableEntity, EntityPack, is } from '../../../Framework/Signum.React/Scripts/Signum.Entities';
import { ifError } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet, ajaxGetRaw, saveFile, ServiceError } from '../../../Framework/Signum.React/Scripts/Services';
import * as Services from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import { tasks, LineBase, LineBaseProps } from '../../../Framework/Signum.React/Scripts/Lines/LineBase'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import ButtonBar from '../../../Framework/Signum.React/Scripts/Frames/ButtonBar'
import { PseudoType, QueryKey, getTypeInfo, PropertyRouteType, OperationInfo, isQueryDefined, getQueryInfo } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { UserEntity, RoleEntity, UserOperation, PermissionSymbol, PropertyAllowed, TypeAllowedBasic, AuthAdminMessage, BasicPermission } from './Signum.Entities.Authorization'
import { PermissionRulePack, TypeRulePack, OperationRulePack, PropertyRulePack, QueryRulePack} from './Signum.Entities.Authorization'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import Login from './Login/Login';

export let userTicket: boolean;
export let resetPassword: boolean;


Services.AuthTokenFilter.addAuthToken = addAuthToken;

export function startPublic(options: { routes: JSX.Element[], userTicket: boolean, resetPassword: boolean }) {
    userTicket = options.userTicket;
    resetPassword = options.resetPassword;

    options.routes.push(<Route path="auth">
        <Route path="login" getComponent={(loc, cb) => require(["./Login/Login"], (Comp) => cb(undefined, Comp.default))}/>
        <Route path="changePassword" getComponent={(loc, cb) => require(["./Login/ChangePassword"], (Comp) => cb(undefined, Comp.default)) }/>
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

    Navigator.addSettings(new EntitySettings(UserEntity, e => new ViewPromise(resolve => require(['./Templates/User'], resolve))));
    Navigator.addSettings(new EntitySettings(RoleEntity, e => new ViewPromise(resolve => require(['./Templates/Role'], resolve))));
    Operations.addSettings(new EntityOperationSettings(UserOperation.SetPassword, { isVisible: ctx => false }));

    if (options.properties) {
        tasks.push(taskAuthorizeProperties);

        Navigator.addSettings(new EntitySettings(PropertyRulePack, e => new ViewPromise(resolve => require(['./Admin/PropertyRulePackControl'], resolve))));
    }

    if (options.types) {
        Navigator.isCreableEvent.push(navigatorIsCreable);
        Navigator.isReadonlyEvent.push(navigatorIsReadOnly);
        Navigator.isViewableEvent.push(navigatorIsViewable);

        Navigator.addSettings(new EntitySettings(TypeRulePack, e => new ViewPromise(resolve => require(['./Admin/TypeRulePackControl'], resolve))));

        QuickLinks.registerQuickLink(RoleEntity, ctx => new QuickLinks.QuickLinkAction("types", AuthAdminMessage.TypeRules.niceToString(),
            e => Api.fetchTypeRulePack(ctx.lite.id!).then(pack => Navigator.navigate(pack)).done(),
            { isVisible: isPermissionAuthorized(BasicPermission.AdminRules) }));
    }

    if (options.operations) {
        Operations.isOperationAllowedEvent.push(onOperationAuthorized);

        Navigator.addSettings(new EntitySettings(OperationRulePack, e => new ViewPromise(resolve => require(['./Admin/OperationRulePackControl'], resolve))));
    }

    if (options.queries) {
        Finder.isFindableEvent.push(queryIsFindable);

        Navigator.addSettings(new EntitySettings(QueryRulePack, e => new ViewPromise(resolve => require(['./Admin/QueryRulePackControl'], resolve))));
    }

    if (options.permissions) {

        Navigator.addSettings(new EntitySettings(PermissionRulePack, e => new ViewPromise(resolve => require(['./Admin/PermissionRulePackControl'], resolve))));

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

export function onOperationAuthorized(oi: OperationInfo) {
    const member = getTypeInfo(oi.key.before(".")).members[oi.key.after(".")];
    return member.operationAllowed;
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
        return false;

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

export const onCurrentUserChanged: Array<(newUser: UserEntity | undefined) => void> = [];

export function setCurrentUser(user: UserEntity | undefined) {

    const changed = !is(Navigator.currentUser, user, true);

    Navigator.setCurrentUser(user);

    if (changed)
        onCurrentUserChanged.forEach(f => f(user));
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
            return u;
        });

    return new Promise<UserEntity>((resolve) => {
        setTimeout(() => {
            if (getAuthToken()) {
                Api.fetchCurrentUser()
                    .then(u => {
                        setCurrentUser(u);
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
                            resolve(respo.userEntity);
                        }
                    });
            }
        }, 500);
    });
}

export function logout() {

    Api.logout().then(() => {
        Options.onLogout();
        setAuthToken(undefined);
        setCurrentUser(undefined);
    }).done();
}

export namespace Options {
    export let onLogout = () => {
        Navigator.currentHistory.push("~/");
    }

    export let onLogin = () => {
        Navigator.currentHistory.push("~/");
    }
}

export function isPermissionAuthorized(permission: PermissionSymbol) {
    const member = getTypeInfo(permission.key.before(".")).members[permission.key.after(".")];
    return member.permissionAllowed;
}

export function asserPermissionAuthorized(permission: PermissionSymbol) {
    if (!isPermissionAuthorized(permission))
        throw new Error(`Permission ${permission.key} is denied`);
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


