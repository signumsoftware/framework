import * as React from 'react'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { ValueSearchControlLine } from '@framework/Search'
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { Entity } from '@framework/Signum.Entities'
import { PropertyRouteEntity } from '@framework/Signum.Entities.Basics'
import * as Constructor from '@framework/Constructor'
import * as DynamicClientOptions from './DynamicClientOptions'
import { globalModules } from './View/GlobalModules'
import { DynamicClientEntity } from './Signum.Entities.Dynamic'

export function start(options: { routes: JSX.Element[] }) {
  Navigator.addSettings(new EntitySettings(DynamicClientEntity, w => import('./Client/DynamicClientComponent')));


  DynamicClientOptions.Options.registerDynamicPanelSearch(DynamicClientEntity, t => [
    { token: t.entity(p => p.name), type: "Text" },
    { token: t.entity(p => p.code), type: "Code" },
  ]);
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
    return ajaxGet({ url: `~/api/dynamic/clients` });
  }
}
