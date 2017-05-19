import * as React from 'react'
import { RestLogEntity, RestApiKeyEntity } from './Signum.Entities.Rest'
import { EntitySettings, ViewPromise } from "../../../Framework/Signum.React/Scripts/Navigator";
import * as Navigator from "../../../Framework/Signum.React/Scripts/Navigator";
import { ajaxGet } from "../../../Framework/Signum.React/Scripts/Services";

export function start(options: { routes: JSX.Element[] }) {
    Navigator.addSettings(new EntitySettings(RestLogEntity, e => _import('./Templates/RestLog')));
    Navigator.addSettings(new EntitySettings(RestApiKeyEntity, e => _import('./Templates/RestApiKey')));
}

export module API {
    export module RestApiKey {
        export function generate(): Promise<string> {
            return ajaxGet<string>({ url: "~/api/restApiKey" });
        }
    }
}
