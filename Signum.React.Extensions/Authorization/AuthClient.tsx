import * as React from 'react'
import { Route } from 'react-router'
import { ModifiableEntity, EntityPack } from '../../../Framework/Signum.React/Scripts/Signum.Entities';
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings } from '../../../Framework/Signum.React/Scripts/Navigator'
import { tasks, LineBase, LineBaseProps } from '../../../Framework/Signum.React/Scripts/Lines/LineBase'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, getTypeInfo, PropertyRouteType, OperationInfo } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { UserEntity, RoleEntity, UserOperation, PermissionSymbol, PropertyAllowed, TypeAllowedBasic } from './Signum.Entities.Authorization'
import Login from './Login/Login';

export let userTicket: boolean;
export let resetPassword: boolean;



export function startPublic(options: { routes: JSX.Element[], userTicket: boolean, resetPassword: boolean }) {
    userTicket = options.userTicket;
    resetPassword = options.resetPassword;

    options.routes.push(<Route path="auth">
        <Route path="login" getComponent={(loc, cb) => require(["./Login/Login"], (Comp) => cb(null, Comp.default))}/>
        <Route path="about" />
    </Route>);
}

export function start(options: { routes: JSX.Element[], types: boolean; properties: boolean, operations: boolean, queries: boolean }) {

    Navigator.addSettings(new EntitySettings(UserEntity, e => new Promise(resolve => require(['./Templates/User'], resolve))));
    Navigator.addSettings(new EntitySettings(RoleEntity, e => new Promise(resolve => require(['./Templates/Role'], resolve))));

    if (options.properties) {
        tasks.push(taskAuthorizeProperties);
    }

    if (options.types) {
        Navigator.isCreableEvent.push(navigatorIsCreable);
        Navigator.isReadonlyEvent.push(navigatorIsReadOnly);
        Navigator.isViewableEvent.push(navigatorIsViewable);
    }

    if (options.operations) {
        Operations.isOperationAllowedEvent.push(onOperationAuthorized);
    }

    if (options.queries) {
        Finder.isFindableEvent.push(queryIsFindable);
    }
}

export function queryIsFindable(queryKey: PseudoType | QueryKey) {
    if (queryKey instanceof QueryKey) {
        return queryKey.memberInfo().queryAllowed;
    } else {
        return getTypeInfo(queryKey).queryAllowed;
    }
}

export function onOperationAuthorized(oi: OperationInfo) {
    var member = getTypeInfo(oi.key.before(".")).members[oi.key.after(".")];
    return member.operationAllowed;
}

export function taskAuthorizeProperties(lineBase: LineBase<LineBaseProps, LineBaseProps>, state: LineBaseProps) {
    if (state.ctx.propertyRoute &&
        state.ctx.propertyRoute.propertyRouteType == PropertyRouteType.Field) {

        var member = state.ctx.propertyRoute.member;

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
    var ti = getTypeInfo(typeName);

    if (ti == null)
        return false;

    if (entityPack && entityPack.typeAllowed)
        return entityPack.typeAllowed == "None" || entityPack.typeAllowed == "Read";

    return ti.typeAllowed == "None" || ti.typeAllowed == "Read";
}

export function navigatorIsViewable(typeName: string, entityPack?: EntityPack<ModifiableEntity>) {
    var ti = getTypeInfo(typeName);

    if (ti == null)
        return false;

    if (entityPack && entityPack.typeAllowed)
        return entityPack.typeAllowed != "None";

    return ti.typeAllowed != "None";
}

export function navigatorIsCreable(typeName: string) {
    var ti = getTypeInfo(typeName);
  
    return ti == null || ti.typeAllowed == "Create";
}

export function currentUser(): UserEntity {
    return Navigator.currentUser as UserEntity;
}

export var onCurrentUserChanged: Array<(newUser: UserEntity) => void> = [];

export function setCurrentUser(user: UserEntity) {
    Navigator.currentUser = user;

    onCurrentUserChanged.forEach(f => f(user));
}


export function logout() {

    Api.logout().then(() => {

        setCurrentUser(null);

        onLogout();
    }).done();
}

export function onLogout() {
    Navigator.currentHistory.push("/");
}

export function onLogin() {
    Navigator.currentHistory.push("/");
}

export function isPermissionAuthorized(permission: PermissionSymbol) {
    var member = getTypeInfo(permission.key.before(".")).members[permission.key.after(".")];
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
        userEntity: UserEntity 
    }

    export function login(loginRequest: LoginRequest): Promise<LoginResponse> {
        return ajaxPost<LoginResponse>({ url: "/api/auth/login" }, loginRequest);
    }

    export function retrieveCurrentUser(): Promise<UserEntity> {
        return ajaxGet<UserEntity>({ url: "/api/auth/currentUser", cache: "no-cache" });
    }

    export function logout(): Promise<void> {
        return ajaxPost<void>({ url: "/api/auth/logout" }, null);
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
        typeAllowed: TypeAllowedBasic;
    }
}


