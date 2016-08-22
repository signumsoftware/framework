import * as React from 'react'
import { RestRequestEntity } from './Signum.Entities.RestLogging'
import { addSettings, EntitySettings } from "../../../Framework/Signum.React/Scripts/Navigator";

export function start(options: { routes: JSX.Element[] }) {
    addSettings(new EntitySettings(RestRequestEntity, e => new Promise(resolve => require(['./Templates/Request'], resolve))));
}
