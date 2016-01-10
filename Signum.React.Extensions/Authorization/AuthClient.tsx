/// <reference path="../../../framework/signum.react/scripts/globals.ts" />

import * as React from 'react'
import { Route } from 'react-router'
import { Type, IType, EntityKind } from 'Framework/Signum.React/Scripts/Reflection';
import { ajaxPost, ajaxGet } from 'Framework/Signum.React/Scripts/Services';
import * as Navigator from 'Framework/Signum.React/Scripts/Navigator';
import { UserEntity, UserEntity_Type } from 'Extensions/Signum.React.Extensions/Authorization/Signum.Entities.Authorization'
import Login from 'Extensions/Signum.React.Extensions/Authorization/Templates/Login';

export var userTicket: boolean;
export var resetPassword: boolean;


export var viewPrefix = "Extensions/Signum.React.Extensions/Authorization/Templates/";

export function start(options: { routes: JSX.Element[], userTicket: boolean, resetPassword: boolean }) {
    userTicket = options.userTicket;
    resetPassword = options.resetPassword;

    options.routes.push(<Route path="auth">
        <Route path="login" getComponent={Navigator.asyncLoad(viewPrefix + "Login")} />
        <Route path="about" />
        </Route>);

    Navigator.addSettings(new Navigator.EntitySettings(UserEntity_Type, u=> viewPrefix + "User")); 
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

    export function currentUser(): Promise<UserEntity> {
        return ajaxGet<UserEntity>({ url: "/api/auth/currentUser", cache: "no-cache" });
    }

    export function logout(): Promise<void> {
        return ajaxPost<void>({ url: "/api/auth/logout" }, null);
    }
}



