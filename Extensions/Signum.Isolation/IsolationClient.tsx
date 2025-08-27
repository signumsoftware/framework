import * as React from 'react'
import { RouteObject } from 'react-router'
import * as AppContext from '@framework/AppContext'
import { IsolationEntity, IsolationMessage } from './Signum.Isolation'
import { Lite, liteKey, ModifiableEntity } from '@framework/Signum.Entities'
import { ajaxGet, addContextHeaders } from '@framework/Services'
import { onWidgets, WidgetContext } from '@framework/Frames/Widgets'
import { IsolationWidget } from './IsolationWidget'
import { getColorProviders } from '../Signum.Map/Schema/ClientColorProvider';

export namespace IsolationClient {
  
  export function start(options: { routes: RouteObject[] }): void {
  
    onWidgets.push(getIsolationWidget);
  
    addContextHeaders.push(options => {
  
      const overridenIsolation = getOverridenIsolation();
      if (overridenIsolation) {
        options.headers = {
          ...options.headers,
          "Signum_Isolation": liteKey(overridenIsolation)
        };
      }
    });
  
    getColorProviders.push(smi => import("./IsolationColorProvider").then((c: any) => c.default(smi)));
  
  }
  
  export namespace Options {
    export let onIsolationChange: ((e: React.MouseEvent, isolation: Lite<IsolationEntity> | undefined) => boolean) | null = null;
  }
  
  export function changeOverridenIsolation(e: React.MouseEvent, isolation: Lite<IsolationEntity> | undefined): void {
  
  
    if (Options.onIsolationChange && Options.onIsolationChange(e, isolation))
        return;
  
      if (isolation)
        sessionStorage.setItem('Curr_Isolation', JSON.stringify(isolation));
      else
        sessionStorage.removeItem('Curr_Isolation');
  
      AppContext.resetUI();  
  }
  
  export function getOverridenIsolation(): Lite<IsolationEntity> | undefined {
  
    const value = sessionStorage.getItem('Curr_Isolation');
    if (value == null)
      return undefined;
  
    const isolation = JSON.parse(value) as Lite<IsolationEntity>;
    return isolation;
  }
  
  export function getIsolationWidget(ctx: WidgetContext<ModifiableEntity>): React.ReactElement<any> | undefined {
  
    return IsolationEntity.tryTypeInfo() ? < IsolationWidget wc={ctx} /> : undefined;
  }
  
  
  export namespace API {
    export function isolations(): Promise<Lite<IsolationEntity>[]> {
      return ajaxGet({ url: "/api/isolations" });
    }
  }
}
