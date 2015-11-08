import * as React from 'react'
import { Authorization } from 'Signum.Entities.Extensions'


export var IsSingleSignOn: boolean;
export var UserTicket: boolean;

export function start(isSingleSignOn: boolean, userTicket: boolean) {
    IsSingleSignOn = isSingleSignOn;
    UserTicket = userTicket;
}