import * as React from 'react'
import { DomUtils, Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { FilterOptionParsed, ColumnOption, hasAggregate, withoutAggregate, FilterOption, FindOptions, withoutPinned } from '@framework/FindOptions'
import { ChartRequestModel, ChartMessage, UserChartEntity } from '../Signum.Chart'
import * as ChartClient from '../ChartClient'
import { toFilterOptions } from '@framework/Finder';

import "../Chart.css"
import { ChartScript, ChartRow } from '../ChartClient';
import { ErrorBoundary } from '@framework/Components';

import ReactChart from '../D3Scripts/Components/ReactChart';
import { useAPI } from '@framework/Hooks'
import { FullscreenComponent } from './FullscreenComponent'
import { DashboardFilter } from '../../Dashboard/View/DashboardFilterController'
import { toAbsoluteUrl } from '@framework/AppContext'
import * as UserQueryClient from '../../Signum.UserQueries/UserQueryClient'
import { DynamicTypeConditionSymbolEntity } from '../../Dynamic/Signum.Entities.Dynamic'
import { extractFindOptions } from '../../Signum.UserQueries/UserQueryClient'
import { Lite } from '@framework/Signum.Entities'

export interface ChartRendererProps {
  userChart?: Lite<UserChartEntity>;
  chartRequest: ChartRequestModel;
  loading: boolean;

  data?: ChartClient.ChartTable;
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
          onRenderChart={cs.chartComponent as ((p: ChartClient.ChartScriptProps) => React.ReactNode)}
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

  UserQueryClient.onDrilldownUserChart(cr, r, uc, { openInNewTab: newWindow, onReload })
    .then(done => {
      if (done == false) {
  if (r.entity) {
    if (newWindow)
      window.open(toAbsoluteUrl(Navigator.navigateRoute(r.entity)));
    else
      Navigator.view(r.entity)
              .then(() => onReload?.());
  } else {
          const fo = extractFindOptions(cr, r);
    if (newWindow)
      window.open(toAbsoluteUrl(Finder.findOptionsPath(fo)));
    else
      Finder.explore(fo)
              .then(() => onReload?.());
        }
  }
    });
}
