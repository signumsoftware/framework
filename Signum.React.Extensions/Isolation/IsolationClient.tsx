import * as React from 'react'
import * as AppContext from '@framework/AppContext'
import { IsolationEntity, IsolationMessage } from './Signum.Entities.Isolation'
import { Lite, liteKey, ModifiableEntity } from '@framework/Signum.Entities'
import { ajaxGet, addContextHeaders } from '@framework/Services'
import { onWidgets, WidgetContext } from '@framework/Frames/Widgets'
import { IsolationWidget } from './IsolationWidget'


export function start(options: { routes: JSX.Element[] }) {

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
}

export function changeOverridenIsolation(isolation: Lite<IsolationEntity> | undefined) {

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


export module API {
  export function isolations(): Promise<Lite<IsolationEntity>[]> {
    return ajaxGet({ url: "~/api/isolations" });
  }
}
