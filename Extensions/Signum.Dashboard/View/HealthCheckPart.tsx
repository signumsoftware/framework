
import * as React from 'react'
import { HealthCheckElementEmbedded, HealthCheckPartEntity, HealthCheckStatus } from '../Signum.Dashboard'
import { PanelPartContentProps } from '../DashboardClient';
import { TypeContext } from '../../../Signum/React/Lines';
import { useAPI } from '../../../Signum/React/Hooks';
import { ajaxGet } from '../../../Signum/React/Services';
import * as AppContext from '@framework/AppContext'
import { softCast } from '../../../Signum/React/Globals';
import { Color } from '@framework/Basics/Color'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'

export default function HealthCheckPart(p: PanelPartContentProps<HealthCheckPartEntity>): React.JSX.Element {
  return (
    <div className="d-flex flex-wrap mt-5">
      {
        p.content.items.map(mle => mle.element).map((le, i) => <HealthCheckElement key={i} element={le} />)
      }
    </div>
  );
}

interface StatusInfo {
  status: HealthCheckStatus;
  httpError: boolean;
  message?: string;
}

function HealthCheckElement(p: { element: HealthCheckElementEmbedded }) {


  var statusInfo = useAPI(() => ajaxGet<HealthCheckStatus>({ url: p.element.checkURL }).then(result => {
    return softCast<StatusInfo>({ status: result, httpError: false });
  }).catch((e) => {
    return softCast<StatusInfo>({ status: 'Error', httpError: true, message: e.message });
  }), [p.element.checkURL]);
  var bgc = statusInfo === undefined ? '#00000038' : statusInfo.status == HealthCheckStatus.value('Ok') ? '#39b54a' : statusInfo.httpError ? '#ec4205' : '#eba70e';
  return (
  <div className='d-flex position-relative justify-content-center align-items-center mx-2 my-2 rounded'
    title={statusInfo?.message}
    style={{
      cursor: p.element.navigateURL ? 'pointer' : undefined,
      minWidth: 240,
      minHeight: 80,
      backgroundColor: bgc,
      color: Color.parse(bgc).opositePole().toString(),
      boxShadow: "3px 3px 12px 3px #00000038"
    }} onClick={e => {
      var path = AppContext.toAbsoluteUrl(p.element.navigateURL);
      window.open(path);
    }}>
      <span className='position-absolute top-0 end-0 me-1 mt-1'><FontAwesomeIcon  icon={statusInfo?.httpError ? 'link-slash':'link'} color='lightgray'/></span>
      <strong>{p.element.title}</strong>
    </div>
  );
}
