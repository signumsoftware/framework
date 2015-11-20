/// <reference path="../../../framework/signum.react/scripts/globals.ts" />

import * as React from 'react'
import { Route } from 'react-router'
import { Type, IType, EntityKind } from 'Framework/Signum.React/Scripts/Reflection';
import * as Navigator from 'Framework/Signum.React/Scripts/Navigator';
import { UserEntity } from 'Extensions/Signum.React.Extensions/Authorization/Signum.Entities.Authorization'
import Login from 'Extensions/Signum.React.Extensions/Authorization/Templates/Login';

export var UserTicket: boolean;

export function currentUser(): UserEntity {
    return Navigator.currentUser as UserEntity;
}




export function start(userTicket: boolean, routes: Route[]) {
    UserTicket = userTicket;

    routes.push(<Route path="auth" >
        <Route path="login" component={ Login } />
        <Route path="about" />
        </Route>);
}