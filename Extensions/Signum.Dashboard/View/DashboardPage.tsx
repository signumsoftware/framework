import * as React from 'react'
import { DateTime } from 'luxon'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Link } from 'react-router-dom'
import { Entity, parseLite, getToString, JavascriptMessage, EntityPack, translated } from '@framework/Signum.Entities'
import { Navigator } from '@framework/Navigator'
import { DashboardEntity, DashboardMessage } from '../Signum.Dashboard'
import DashboardView from './DashboardView'
import { useLocation, useParams } from "react-router";
import "../Dashboard.css"
import { useAPI, useAPIWithReload, useInterval } from '@framework/Hooks'
import { QueryString } from '@framework/QueryString'
import { DashboardClient } from "../DashboardClient"
import { newLite } from '@framework/Reflection'
import { OverlayTrigger, Popover } from "react-bootstrap";

export default function DashboardPage() {
  const location = useLocation();
  const params = useParams() as { dashboardId: string };

  const [dashboardWithQueries, reloadDashboard] = useAPIWithReload(signal => DashboardClient.API.get(newLite(DashboardEntity, params.dashboardId)), [params.dashboardId]);

  const dashboard = dashboardWithQueries?.dashboard;

  var entityKey = QueryString.parse(location.search).entity as string;

  const entity = useAPI(signal => entityKey ? Navigator.API.fetch(parseLite(entityKey)) : Promise.resolve(null), [entityKey]);

  const refreshCounter = useInterval(dashboard?.autoRefreshPeriod == null ? null : dashboard.autoRefreshPeriod * 1000, 0, old => old + 1);

  React.useEffect(() => {

    if (dashboardWithQueries && dashboardWithQueries.cachedQueries.length > 0)
      reloadDashboard();

  }, [refreshCounter]);

  var cachedQueries = React.useMemo(() => DashboardClient.toCachedQueries(dashboardWithQueries), [dashboardWithQueries]);

  return (
    <div>
      {!dashboard ? <h2 className="display-5">{JavascriptMessage.loading.niceToString()}</h2> :
        <div className="d-flex">
          {<h2 className="display-5">{DashboardClient.Options.customTitle(dashboard)}</h2>}
          {!Navigator.isReadOnly(DashboardEntity) &&
            <div className="ms-auto">
              {dashboardWithQueries.cachedQueries.length ? <span className="mx-4" title={DashboardMessage.ForPerformanceReasonsThisDashboardMayShowOutdatedInformation.niceToString() + "\n" +
                DashboardMessage.LasUpdateWasOn0.niceToString(DateTime.fromISO(dashboardWithQueries.cachedQueries[0].creationDate).toLocaleString(DateTime.DATETIME_MED_WITH_SECONDS))}>
                <FontAwesomeIcon icon="clock-rotate-left" /> {DateTime.fromISO(dashboardWithQueries.cachedQueries[0].creationDate).toRelative()}
              </span> : null}
              {dashboard.parts.some(a => a.element.interactionGroup != null) && <HelpIcon />}
              <Link className="sf-hide" style={{ textDecoration: "none" }} to={Navigator.navigateRoute(dashboard)}><FontAwesomeIcon icon="pen-to-square" />&nbsp;{DashboardMessage.Edit.niceToString()}</Link>
            </div>
          }
        </div>}

      {entityKey &&
        <div>
          {!entity ? <h3>{JavascriptMessage.loading.niceToString()}</h3> :
            <h3>
              {Navigator.isViewable({ entity: entity, canExecute: {} } as EntityPack<Entity>) ?
                <Link className="display-6" to={Navigator.navigateRoute(entity)}>{getToString(entity)}</Link> :
                <span className="display-6">{getToString(entity)}</span>
              }
              &nbsp;
            <small className="sf-type-nice-name">{Navigator.getTypeSubTitle(entity, undefined)}</small>
            </h3>
          }
        </div>
      }

      {dashboard && (!entityKey || entity) && <DashboardView dashboard={dashboard} cachedQueries={cachedQueries!} entity={entity || undefined} deps={[refreshCounter, entity]} reload={reloadDashboard} hideEditButton={true} />}
    </div>
  );
}

export function HelpIcon() {
  const popover = (
    <Popover id="popover-basic" style={{ "--bs-popover-max-width": "unset" } as React.CSSProperties}>
      <Popover.Header as="h3">Interactive Dashboard</Popover.Header>
      <Popover.Body>
        <ul className="ps-3">
          <li style={{ whiteSpace: "nowrap" }}>{DashboardMessage.CLickInOneChartToFilterInTheOthers.niceToString()}</li>
          <li style={{ whiteSpace: "nowrap" }}>{DashboardMessage.CtrlClickToFilterByMultipleElements.niceToString()}</li>
          <li style={{ whiteSpace: "nowrap" }}>{DashboardMessage.AltClickToOpenResultsInAModalWindow.niceToString()}</li>
        </ul>
      </Popover.Body>
    </Popover>
  );

  return (
    <OverlayTrigger trigger="hover" placement="bottom-start" overlay={popover} >
      <a href="#" className="mx-2"><FontAwesomeIcon icon="gamepad" title="syntax" className="me-1" />Interactive Dashboard</a>
    </OverlayTrigger>
  );

}
