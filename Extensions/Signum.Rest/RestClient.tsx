import * as React from 'react'
import { RouteObject } from 'react-router'
import { RestLogEntity, RestApiKeyEntity } from './Signum.Rest'
import { Navigator, EntitySettings } from "@framework/Navigator";
import { ajaxGet } from "@framework/Services";
import { AuthClient } from "../Signum.Authorization/AuthClient";
import { QueryString } from '@framework/QueryString';

export namespace RestClient {
  
  export function start(options: { routes: RouteObject[] }): void {
    Navigator.addSettings(new EntitySettings(RestLogEntity, e => import('./Templates/RestLog')));
  }
  
  export module API {
  
    export function replayRestLog(restLogID: string | number, host: string) : Promise<string> {
      return ajaxGet({ url: "/api/restLog?id=" + restLogID + "&url=" + host });
  
    }
  }
}
