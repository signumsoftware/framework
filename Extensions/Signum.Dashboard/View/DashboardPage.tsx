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
import { useTitle } from '@framework/AppContext'
import { LinkButton } from '@framework/Basics/LinkButton'

export default function DashboardPage(): React.JSX.Element {
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

  useTitle(entity ? getToString(entity) : getToString(dashboard));


  return (
    <div className="sf-dashboard-page">
    
      {!dashboard ? <h1 className="display-6 h2"><span>{JavascriptMessage.loading.niceToString()}</span></h1> :
        <div className="d-flex">
        <div>
            {entityKey ?
              <div>
                {!entity ? <h1 className="h3">{JavascriptMessage.loading.niceToString()}</h1> :
                  <h1 className="h3">
                    <span className="display-6">{getToString(entity)}</span>
                    {Navigator.isViewable({ entity: entity, canExecute: {} } as EntityPack<Entity>) &&
                      <Link className="display-6 ms-2" to={Navigator.navigateRoute(entity)}><FontAwesomeIcon aria-hidden={true} icon="external-link" /></Link>
                    }
                    <small className="ms-1 sf-type-nice-name text-muted"> - {Navigator.getTypeSubTitle(entity, undefined)}</small>
                  </h1>
                }
                <h2 className="display-7 h4">{DashboardClient.Options.customTitle(dashboard)}</h2>
              </div> :
              <h1 className="display-6 h3">{DashboardClient.Options.customTitle(dashboard)}</h1>
            }
          </div>
          {!Navigator.isReadOnly(DashboardEntity) &&
            <div className="ms-auto">
              {dashboardWithQueries.cachedQueries.length ? <span className="mx-4" title={DashboardMessage.ForPerformanceReasonsThisDashboardMayShowOutdatedInformation.niceToString() + "\n" +
                DashboardMessage.LasUpdateWasOn0.niceToString(DateTime.fromISO(dashboardWithQueries.cachedQueries[0].creationDate).toLocaleString(DateTime.DATETIME_MED_WITH_SECONDS))}>
                <FontAwesomeIcon aria-hidden={true} icon="clock-rotate-left" /> {DateTime.fromISO(dashboardWithQueries.cachedQueries[0].creationDate).toRelative()}
              </span> : null}
              {dashboard.parts.some(a => a.element.interactionGroup != null) && <HelpIcon />}
              <Link className="sf-hide" style={{ textDecoration: "none" }} to={Navigator.navigateRoute(dashboard)} title={DashboardMessage.Edit.niceToString()}>
                <FontAwesomeIcon aria-hidden={true} icon="pen-to-square" />
              </Link>
            </div>
          }
        </div>}

      {dashboard && (!entityKey || entity) && <DashboardView dashboard={dashboard} cachedQueries={cachedQueries!} entity={entity || undefined} deps={[refreshCounter, entity]} reload={reloadDashboard} hideEditButton={true} />}
    </div>
  );
}

export function HelpIcon(): React.JSX.Element {
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
    <OverlayTrigger trigger={["hover", "focus"]} placement="bottom-start" overlay={popover} >
      <LinkButton className="mx-2" title={undefined}><FontAwesomeIcon icon="gamepad" title="syntax" className="me-1" />{DashboardMessage.InteractiveDashboard.niceToString()}</LinkButton>
    </OverlayTrigger>
  );

}
