import * as React from 'react'
import { DateTime } from 'luxon'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Link } from 'react-router-dom'
import { Entity, parseLite, getToString, JavascriptMessage, EntityPack, liteKey } from '@framework/Signum.Entities'
import * as Finder from '@framework/Finder'
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
import { downloadFile } from '../../Files/FileDownloader'
import { CachedQueryJS } from '../CachedQueryExecutor'

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

  var cachedQueries = React.useMemo(() => dashboardWithQueries?.cachedQueries
    .map(a => ({ userAssets: a.userAssets, promise: downloadFile(a.file).then(r => r.json() as Promise<CachedQueryJS>).then(cq => { Finder.decompress(cq.resultTable); return cq; })})) //share promise
    .flatMap(a => a.userAssets.map(mle => ({ ua: mle.element, promise: a.promise })))
    .toObject(a => liteKey(a.ua), a => a.promise), [dashboardWithQueries]);

  return (
    <div>
      {!dashboard ? <h2 className="display-5">{JavascriptMessage.loading.niceToString()}</h2> :
        <div className="sf-show-hover">
          {!Navigator.isReadOnly(DashboardEntity) &&
            <div className="float-end mt-3">
              {dashboardWithQueries.cachedQueries.length ? <span className="mx-4" title={DashboardMessage.ForPerformanceReasonsThisDashboardMayShowOutdatedInformation.niceToString() + "\n" +
                DashboardMessage.LasUpdateWasOn0.niceToString(DateTime.fromISO(dashboardWithQueries.cachedQueries[0].creationDate).toLocaleString(DateTime.DATETIME_MED_WITH_SECONDS))}>
                <FontAwesomeIcon icon="history" /> {DateTime.fromISO(dashboardWithQueries.cachedQueries[0].creationDate).toRelative()}
              </span> : null}
              <Link className="sf-hide " style={{ textDecoration: "none" }} to={Navigator.navigateRoute(dashboard)}><FontAwesomeIcon icon="edit" />&nbsp;Edit</Link>
            </div>
          }
          <h2 className="display-5">{translated(dashboard, d => d.displayName)}</h2>
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
            <small className="sf-type-nice-name">{Navigator.getTypeTitle(entity, undefined)}</small>
            </h3>
          }
        </div>
      }

      {dashboard && (!entityKey || entity) && <DashboardView dashboard={dashboard} cachedQueries={cachedQueries!} entity={entity || undefined} deps={[refreshCounter, entity]} reload={reloadDashboard} />}
    </div>
  );
}
