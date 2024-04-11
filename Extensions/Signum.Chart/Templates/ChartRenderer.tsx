import * as React from 'react'
import { Finder } from '@framework/Finder'
import { Navigator } from '@framework/Navigator'
import { ChartRequestModel} from '../Signum.Chart'

import "../Chart.css"
import { ChartClient, ChartRow, ChartScriptProps, ChartTable } from '../ChartClient';
import { ErrorBoundary } from '@framework/Components';

import ReactChart from '../D3Scripts/Components/ReactChart';
import { useAPI } from '@framework/Hooks'
import { DashboardFilter } from '../../Signum.Dashboard/View/DashboardFilterController'
import { toAbsoluteUrl } from '@framework/AppContext'
import { UserQueryClient } from '../../Signum.UserQueries/UserQueryClient'
import { Lite } from '@framework/Signum.Entities'
import { UserChartEntity } from '../UserChart/Signum.Chart.UserChart'
import { FullscreenComponent } from '@framework/Components/FullscreenComponent'

export interface ChartRendererProps {
  userChart?: Lite<UserChartEntity>;
  chartRequest: ChartRequestModel;
  loading: boolean;

  data?: ChartTable;
  lastChartRequest?: ChartRequestModel;
  onReload?: (e?: React.MouseEvent<any>) => void;
  autoRefresh: boolean;
  dashboardFilter?: DashboardFilter;
  onDrillDown?: (row: ChartRow, e: React.MouseEvent | MouseEvent) => void;
  onBackgroundClick?: (e: React.MouseEvent) => void;
  minHeight: number | null;
}

export default function ChartRenderer(p: ChartRendererProps) {
  const cs = useAPI(async signal => {
    const chartScriptPromise = ChartClient.getChartScript(p.chartRequest.chartScript);
    const chartComponentModulePromise = ChartClient.getRegisteredChartScriptComponent(p.chartRequest.chartScript);

    const chartScript = await chartScriptPromise;
    const chartComponentModule = await chartComponentModulePromise();

    return { chartComponent: chartComponentModule.default, chartScript };
  }, [p.chartRequest.chartScript]);

  var parameters = cs && ChartClient.API.getParameterWithDefault(p.chartRequest, cs.chartScript)

  return (
    <FullscreenComponent onReload={p.onReload}>
      <ErrorBoundary deps={[p.data]}>
        {cs && parameters &&
          <ReactChart
          chartRequest={p.chartRequest}
          data={p.data}
          dashboardFilter={p.dashboardFilter}
          loading={p.loading}
          onDrillDown={p.onDrillDown ?? ((r, e) => handleDrillDown(r, e, p.lastChartRequest!, p.userChart, p.autoRefresh ? p.onReload : undefined))}
          onBackgroundClick={p.onBackgroundClick}
          parameters={parameters}
          onReload={p.onReload}
          onRenderChart={cs.chartComponent as ((p: ChartScriptProps) => React.ReactNode)}
          minHeight={p.minHeight}
        />
        }
      </ErrorBoundary>
    </FullscreenComponent>
  );
}

export function handleDrillDown(r: ChartRow, e: React.MouseEvent | MouseEvent, cr: ChartRequestModel, uc?: Lite<UserChartEntity>, onReload?: () => void) {

  e.stopPropagation();
  var newWindow = e.ctrlKey || e.button == 1;

  ChartClient.onDrilldownUserChart(cr, r, uc, { openInNewTab: newWindow, onReload })
    .then(done => {
      if (done == false) {
  if (r.entity) {
    if (newWindow)
      window.open(toAbsoluteUrl(Navigator.navigateRoute(r.entity)));
    else
      Navigator.view(r.entity)
              .then(() => onReload?.());
  } else {
          const fo = ChartClient.extractFindOptions(cr, r);
    if (newWindow)
      window.open(toAbsoluteUrl(Finder.findOptionsPath(fo)));
    else
      Finder.explore(fo)
              .then(() => onReload?.());
        }
  }
    });
}
