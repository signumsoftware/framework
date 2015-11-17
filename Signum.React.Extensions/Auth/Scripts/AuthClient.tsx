/// <reference path="../../framework/signum.react/scripts/globals.ts" />

import * as React from 'react'
import { Route } from 'react-router'
import { Type, IType, EntityKind } from 'Framework/Signum.React/Scripts/Reflection';
import * as Navigator from 'Framework/Signum.React/Scripts/Navigator';
import { Authorization } from 'Extensions/Signum.React.Extensions/Signum.Entities.Extensions'
import Login from 'Extensions/Signum.React.Extensions/Auth/Templates/Login';

export var IsSingleSignOn: boolean;
export var UserTicket: boolean;

export function currentUser(): Authorization.UserEntity {
    return Navigator.currentUser() as Authorization.UserEntity;
}




export function start(isSingleSignOn: boolean, userTicket: boolean, routes: Route[]) {
    IsSingleSignOn = isSingleSignOn;
    UserTicket = userTicket;


    routes.push(<Route path="auth" >
        <Route path="login" component={ Login } />
        <Route path="about" />
        </Route>);



}