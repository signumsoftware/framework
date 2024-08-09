
import * as React from 'react'
import { HealthCheckElementEmbedded, HealthCheckPartEntity, HealthCheckStatus } from '../Signum.Dashboard'
import { PanelPartContentProps } from '../DashboardClient';
import { TypeContext } from '../../../Signum/React/Lines';
import { useAPI } from '../../../Signum/React/Hooks';
import { ajaxGet } from '../../../Signum/React/Services';
import * as AppContext from '@framework/AppContext'
import { softCast } from '../../../Signum/React/Globals';

export default function HealthCheckPart(p: PanelPartContentProps<HealthCheckPartEntity>): React.JSX.Element {
  return (
    <div className="d-flex mt-5">
      {
        p.content.items.map(mle => mle.element).map((le, i) => <HealthCheckElement key={i} element={le} />)
      }
    </div>
  );
}

interface StatusInfo {
  status: HealthCheckStatus;
  message?: string;
}

function HealthCheckElement(p: { element: HealthCheckElementEmbedded }) {


  var statusInfo = useAPI(() => ajaxGet<HealthCheckStatus>({ url: p.element.checkURL }).then(result => {
    return softCast<StatusInfo>({ status: result, message: 'Server call was OK.' });
  }).catch((e) => {
    return softCast<StatusInfo>({ status: 'Error', message: e.message });
  }), [p.element.checkURL]);

  return (<div className='d-flex justify-content-center align-items-center mx-2 my-2'
    title={statusInfo?.message}
    style={{
      cursor: p.element.navigateURL ? 'pointer' : undefined,
      minWidth: 240, minHeight: 80,
      backgroundColor: statusInfo === undefined ? 'lightgray' : statusInfo.status == HealthCheckStatus.value('Ok') ? 'lightgreen' : 'red', borderRadius: 5
    }} onClick={e => {
      var path = AppContext.toAbsoluteUrl(p.element.navigateURL);
      window.open(path);
    }}>
    <strong>{p.element.title}</strong>
  </div>);
}
