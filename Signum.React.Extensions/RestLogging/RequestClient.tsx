import * as React from 'react'
import { RequestEntity } from './Signum.Entities.RestLogging'
import { addSettings, EntitySettings } from "../../../Framework/Signum.React/Scripts/Navigator";

export function start(options: { routes: JSX.Element[] }) {
    addSettings(new EntitySettings(RequestEntity, e => new Promise(resolve => require(['./Templates/Request'], resolve))));
}
