/// <reference path="../../../framework/signum.react/scripts/globals.ts" />

import * as React from 'react'
import { Route } from 'react-router'
import { Type, IType, EntityKind, TypeInfoDictionary } from '../../../Framework/Signum.React/Scripts/Reflection';
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { addSettings, EntitySettings } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { UserEntity, UserEntity_Type, RoleEntity_Type } from './Signum.Entities.Authorization'
import Login from './Login/Login';

export let userTicket: boolean;
export let resetPassword: boolean;





export function start(options: { routes: JSX.Element[], userTicket: boolean, resetPassword: boolean }) {
    userTicket = options.userTicket;
    resetPassword = options.resetPassword;

    options.routes.push(<Route path="auth">
        <Route path="login" getComponent={(loc, cb) => require(["./Login/Login"], (Comp) => cb(null, Comp.default)) } />
        <Route path="about" />
    </Route>);

    addSettings(new EntitySettings(UserEntity_Type, e => new Promise(resolve => require(['./Templates/User'], resolve))));
    addSettings(new EntitySettings(RoleEntity_Type, e => new Promise(resolve => require(['./Templates/Role'], resolve))));
}

export function currentUser(): UserEntity {
    return Navigator.currentUser as UserEntity;
}

export const CurrentUserChangedEvent = "current-user-changed";
export function setCurrentUser(user: UserEntity) {
    Navigator.currentUser = user;

    document.dispatchEvent(new Event(CurrentUserChangedEvent));
}


export function logout() {

    Api.logout().then(() => {

        setCurrentUser(null);

        onLogout();
    });
}

export function onLogout() {
    Navigator.currentHistory.push("/");
}

export function onLogin() {
    Navigator.currentHistory.push("/");
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

    export function basicTypes(): Promise<TypeInfoDictionary> {
        return ajaxGet<TypeInfoDictionary>({ url: "/api/auth/basicTypes" });
    }
}



