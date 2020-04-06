import * as React from 'react'
import * as moment from 'moment'
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { EntityOperationSettings } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import SelectorModal from '@framework/SelectorModal'
import ValueLineModal from '@framework/ValueLineModal'
import * as QuickLinks from '@framework/QuickLinks'
import { andClose } from '@framework/Operations/EntityOperations';
import * as AuthClient from '../Authorization/AuthClient'
import { IsolationEntity } from './Signum.Entities.Isolation'
import { Lite, liteKey, ModifiableEntity } from '@framework/Signum.Entities'
import { ajaxGet, addContextHeaders } from '../../../Framework/Signum.React/Scripts/Services'
import { CultureInfoEntity } from '../Basics/Signum.Entities.Basics'
import { onWidgets, WidgetContext } from '../../../Framework/Signum.React/Scripts/Frames/Widgets'
import { IsolationWidget } from './IsolationWidget'


export function start(options: { routes: JSX.Element[], couldHaveAlerts?: (typeName: string) => boolean }) {
  //Navigator.addSettings(new EntitySettings(AlertTypeEntity, e => import('./Templates/AlertType')));

  onWidgets.push(getIsolationWidget);

  addContextHeaders.push(options => {
    if (overridenIsolation) {
      options.headers = {
        ...options.headers,
        "SF_Isolation": liteKey(overridenIsolation)
      };
    }
  });
}

export let overridenIsolation: Lite<IsolationEntity> | undefined;

export function changeOverridenIsolation(isolation: Lite<IsolationEntity> | undefined) {
  overridenIsolation = isolation;
  Navigator.resetUI();
}

export function getIsolationWidget(ctx: WidgetContext<ModifiableEntity>): React.ReactElement<any> {

  return <IsolationWidget wc={ctx} />;
}


export module API {
  export function isolations(): Promise<Lite<IsolationEntity>[]> {
    return ajaxGet({ url: "~/api/isolations" });
  }
}
