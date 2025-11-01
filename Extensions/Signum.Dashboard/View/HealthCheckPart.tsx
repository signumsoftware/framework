
import * as React from 'react'
import { HealthCheckElementEmbedded, HealthCheckPartEntity } from '../Signum.Dashboard'
import { DashboardClient, PanelPartContentProps } from '../DashboardClient';
import { TypeContext } from '../../../Signum/React/Lines';
import { useAPI } from '../../../Signum/React/Hooks';
import { ajaxGet, ServiceError } from '../../../Signum/React/Services';
import * as AppContext from '@framework/AppContext'
import { softCast } from '../../../Signum/React/Globals';
import { Color } from '@framework/Basics/Color'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { JavascriptMessage } from '@framework/Signum.Entities';
import { fallbackIcon, parseIcon } from '@framework/Components/IconTypeahead';
import DashboardPage from './DashboardPage';

export default function HealthCheckPart(p: PanelPartContentProps<HealthCheckPartEntity>): React.JSX.Element {
  const part = p.partEmbedded;
  const defaultIcon = DashboardClient.partRenderers[HealthCheckPartEntity.typeName].icon?.();
  const icon = parseIcon(part.iconName) ?? defaultIcon?.icon;
  const iconColor = part.iconColor ?? defaultIcon?.iconColor;

  const title = !icon ? part.title :
    <span>
      <FontAwesomeIcon aria-hidden={true} icon={fallbackIcon(icon)} color={iconColor} className="me-1" />{part.title}
    </span>;

  return (
    <div className="my-3">
      <h5 style={{ color: p.partEmbedded.titleColor ?? undefined }} >{title}</h5>
      <div className="d-flex flex-wrap">
        {
          p.content.items.map(mle => mle.element).map((le, i) => <HealthCheckElement key={i} element={le} />)
        }
      </div>
    </div>
  );
}

type HealthResult = "Healthy" | "Degraded" | "Unhealthy";

interface HealthCheckResult {
  status: HealthResult;
  description: string;
  data?: Record<string, any>;
}

type StatusInfo = { result: HealthCheckResult } | { error: any };

function HealthCheckElement(p: { element: HealthCheckElementEmbedded }) {

  var data = useAPI(() => ajaxGet<HealthCheckResult>({
    url: p.element.checkURL,
    avoidAuthToken: true,
    avoidContextHeaders: true,
    avoidVersionCheck: true,
  }).then(result => {
    return softCast<StatusInfo>({ result });
  }).catch((e) => {

    if (e instanceof ServiceError && "status" in e.httpError && "description" in e.httpError)
      return softCast<StatusInfo>({ result: e.httpError as HealthCheckResult });

    return softCast<StatusInfo>({ error: e });
  }), [p.element.checkURL]);

  const bgc = data === undefined ? '#00000038' :
    "error" in data ? "#ec4205" :
      data.result.status == "Healthy" ? (data.result.description == "Disabled" ? "#eee" : "#6ecb7b") :
        data.result.status == "Degraded" ? "#ffd43f" :
          data.result.status == "Unhealthy" ? "#ec4205" : "#f700ff";

  const color = Color.parse(bgc);
  const message = data == null ? JavascriptMessage.loading.niceToString() :
    "error" in data ? (data.error instanceof ServiceError ? data.error.httpError.exceptionMessage : data.error.message ?? data.error) :
      data.result.description;

  const foreColor = color.lerp(.5, Color.parse(bgc).opositePole()).toString();

  return (
    <div className='d-flex position-relative justify-content-center align-items-center mx-2 my-2 rounded'
      title={message}
    style={{
      cursor: p.element.navigateURL ? 'pointer' : undefined,
      minWidth: 240,
      minHeight: 80,
      backgroundColor: bgc,
      color: foreColor,
      boxShadow: "3px 3px 12px 3px " + color.withAlpha(.2).toString(),
      textAlign: "center",
    }} onClick={e => {
      var path = AppContext.toAbsoluteUrl(p.element.navigateURL);
      window.open(path);
    }}>
      <span className='position-absolute top-0 end-0 me-1 mt-1'>
        <FontAwesomeIcon aria-hidden={true} icon={data == null ? "hourglass-start" :
          "error" in data ? 'link-slash' :
            data.result.status == "Healthy" ? (data.result.description == "Disabled" ? "circle" : "circle-check") :
              data.result.status == "Degraded" ? "circle-down" :
                data.result.status == "Unhealthy" ? "circle-xmark" :
                  'link'} color={foreColor} className="fs-2" />
      </span>
      <strong>{p.element.title}</strong>
    </div>
  );
}
