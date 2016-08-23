import * as React from 'react'
import { RestLogEntity } from './Signum.Entities.RestLog'
import { addSettings, EntitySettings } from "../../../Framework/Signum.React/Scripts/Navigator";

export function start(options: { routes: JSX.Element[] }) {
    addSettings(new EntitySettings(RestLogEntity, e => new Promise(resolve => require(['./Templates/RestLog'], resolve))));
}
