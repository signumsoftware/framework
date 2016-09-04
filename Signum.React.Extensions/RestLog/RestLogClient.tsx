import * as React from 'react'
import { RestLogEntity } from './Signum.Entities.RestLog'
import { EntitySettings, ViewPromise } from "../../../Framework/Signum.React/Scripts/Navigator";
import * as Navigator from "../../../Framework/Signum.React/Scripts/Navigator";

export function start(options: { routes: JSX.Element[] }) {
    Navigator.addSettings(new EntitySettings(RestLogEntity, e => new ViewPromise(resolve => require(['./Templates/RestLog'], resolve))));
}
