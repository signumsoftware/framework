import * as React from 'react'
import { DomUtils, Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { FilterOptionParsed, ColumnOption, hasAggregate, withoutAggregate, FilterOption, FindOptions, withoutPinned } from '@framework/FindOptions'
import { ChartRequestModel, ChartMessage } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import { toFilterOptions } from '@framework/Finder';

import "../Chart.css"
import { ChartScript, ChartRow } from '../ChartClient';
import { ErrorBoundary } from '@framework/Components';

import ReactChart from '../D3Scripts/Components/ReactChart';
import { useAPI } from '@framework/Hooks'
import { FullscreenComponent } from './FullscreenComponent'
import { DashboardFilter } from '../../Dashboard/View/DashboardFilterController'
import * as UserQueryClient from '../../UserQueries/UserQueryClient'
import { DynamicTypeConditionSymbolEntity } from '../../Dynamic/Signum.Entities.Dynamic'
import { handleCustomDrilldowns } from '../../UserAssets/UserAssetClient'


export interface ChartRendererProps {
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
          onDrillDown={p.onDrillDown ?? ((r, e) => handleDrillDown(r, e, p.lastChartRequest!, p.autoRefresh ? p.onReload : undefined))}
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

export function handleDrillDown(r: ChartRow, e: React.MouseEvent | MouseEvent, cr: ChartRequestModel, onReload?: () => void) {

  e.stopPropagation();
  var newWindow = e.ctrlKey || e.button == 1;
  const customDrilldowns = cr.customDrilldowns.map(mle => mle.element);
  const fo = extractFindOptions(cr, r);
  const entity = r.entity ?? (ChartClient.hasAggregates(cr) ? undefined : fo.filterOptions?.singleOrNull(f => f?.token == "Entity")?.value);
  if (entity) {
    if (customDrilldowns.length > 0)
      return handleCustomDrilldowns(customDrilldowns, { openInNewTab: newWindow, entity, onReload });

    if (newWindow)
      window.open(Navigator.navigateRoute(entity));
    else
      Navigator.view(entity)
        .then(() => onReload?.());
  } else {
    if (customDrilldowns.length > 0)
      return handleCustomDrilldowns(customDrilldowns, { openInNewTab: newWindow, fo, onReload });

    if (newWindow)
      window.open(Finder.findOptionsPath(fo));
    else
      Finder.explore(fo)
        .then(() => onReload?.());
  }
}

function extractFindOptions(cr: ChartRequestModel, r: ChartRow) {

  const filters = cr.filterOptions.map(f => {
    let f2 = withoutPinned(f);
    if (f2 == null)
      return null;
    return withoutAggregate(f2);
  }).notNull();

  const columns: ColumnOption[] = [];

  cr.columns.map((a, i) => {

    const qte = a.element.token;

    if (qte?.token && !hasAggregate(qte!.token!) && r.hasOwnProperty("c" + i)) {
      filters.push({
        token: qte!.token!,
        operation: "EqualTo",
        value: (r as any)["c" + i],
        frozen: false
      } as FilterOptionParsed);
    }

    if (qte?.token && qte.token.parent != undefined) //Avoid Count and simple Columns that are already added
    {
      var t = qte.token;
      if (t.queryTokenType == "Aggregate") {
        columns.push({
          token: t.parent!.fullKey,
          summaryToken: t.fullKey
        });
      } else {
        columns.push({
          token: t.fullKey,
        });
      }
    }
  });

  var fo: FindOptions = {
    queryName: cr.queryKey,
    filterOptions: toFilterOptions(filters),
    includeDefaultFilters: false,
    columnOptions: columns,
    columnOptionsMode: "ReplaceOrAdd",
  };

  return fo;
}
