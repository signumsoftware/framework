import * as React from 'react'
import { DateTime } from 'luxon'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Link } from 'react-router-dom'
import { Entity, parseLite, getToString, JavascriptMessage, EntityPack } from '@framework/Signum.Entities'
import * as Navigator from '@framework/Navigator'
import { DashboardEntity, DashboardMessage } from '../Signum.Entities.Dashboard'
import DashboardView from './DashboardView'
import { RouteComponentProps } from "react-router";
import "../Dashboard.css"
import { useAPI, useAPIWithReload, useInterval } from '@framework/Hooks'
import { QueryString } from '@framework/QueryString'
import { translated } from '../../Translation/TranslatedInstanceTools'
import * as DashboardClient from "../DashboardClient"
import { newLite } from '@framework/Reflection'

interface DashboardPageProps extends RouteComponentProps<{ dashboardId: string }> {

}

function getQueryEntity(props: DashboardPageProps): string {
  return QueryString.parse(props.location.search).entity as string;
}

export default function DashboardPage(p: DashboardPageProps) {

  const [dashboardWithQueries, reloadDashboard] = useAPIWithReload(signal => DashboardClient.API.get(newLite(DashboardEntity, p.match.params.dashboardId)), [p.match.params.dashboardId]);

  const dashboard = dashboardWithQueries?.dashboard;

  var entityKey = getQueryEntity(p);

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
          {!dashboard.hideDisplayName && <h2 className="display-5">{translated(dashboard, d => d.displayName)}</h2>}
          {!Navigator.isReadOnly(DashboardEntity) &&
            <div className="ms-auto">
              {dashboardWithQueries.cachedQueries.length ? <span className="mx-4" title={DashboardMessage.ForPerformanceReasonsThisDashboardMayShowOutdatedInformation.niceToString() + "\n" +
                DashboardMessage.LasUpdateWasOn0.niceToString(DateTime.fromISO(dashboardWithQueries.cachedQueries[0].creationDate).toLocaleString(DateTime.DATETIME_MED_WITH_SECONDS))}>
                <FontAwesomeIcon icon="clock-rotate-left" /> {DateTime.fromISO(dashboardWithQueries.cachedQueries[0].creationDate).toRelative()}
              </span> : null}
              <Link className="sf-hide " style={{ textDecoration: "none" }} to={Navigator.navigateRoute(dashboard)}><FontAwesomeIcon icon="pen-to-square" />&nbsp;Edit</Link>
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

      {dashboard && (!entityKey || entity) && <DashboardView dashboard={dashboard} cachedQueries={cachedQueries!} entity={entity || undefined} deps={[refreshCounter, entity]} reload={reloadDashboard} />}
    </div>
  );
}
