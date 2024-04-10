import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { SearchValueLine } from '@framework/Search'
import { Navigator, EntitySettings } from '@framework/Navigator'
import { Entity } from '@framework/Signum.Entities'
import { PropertyRouteEntity } from '@framework/Signum.Basics'
import { Constructor } from '@framework/Constructor'
import { EvalClient } from '../Signum.Eval/EvalClient'
import { globalModules } from './View/GlobalModules'
import { DynamicClientEntity } from './Signum.Dynamic.Client';

export namespace DynamicClientClient {
  
  export function start(options: { routes: RouteObject[] }) {
    Navigator.addSettings(new EntitySettings(DynamicClientEntity, w => import('./Client/DynamicClientComponent')));
  
  
    EvalClient.Options.registerDynamicPanelSearch(DynamicClientEntity, t => [
      { token: t.append(p => p.entity.name), type: "Text" },
      { token: t.append(p => p.entity.code), type: "Code" },
    ]);
  }
  
  export namespace Options {
    export let getDynaicMigrationsStep: (() => React.ReactElement<any>) | undefined = undefined;
  
  }
  
  //Run before reload
  export function getIsSafeMode() {
    return window.location.search.contains("safeMode");
  }
  
  export function startDynamicClientsAsync(): Promise<void> {
    return API.getClients().then(cs => {
  
      cs.forEach(c => {
        try {
          var start = eval(`(function start${c.name}(modules){
  ${c.code}
  })`);
          start(globalModules);
        } catch (e) {
          console.error(`Error in DynamicClient (${c.name}), consider using ${window.location}?safeMode`);
          console.error(e);
        }
      });
    })
  }
  
  export namespace API {
    export function getClients(): Promise<DynamicClientEntity[]> {
      return ajaxGet({ url: `/api/dynamic/clients` });
    }
  }
}
