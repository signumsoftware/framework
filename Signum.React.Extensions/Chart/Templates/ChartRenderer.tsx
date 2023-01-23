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
import SelectorModal from '../../../Signum.React/Scripts/SelectorModal'
import { UserQueryEntity } from '../../UserQueries/Signum.Entities.UserQueries'
import * as UserQueryClient from '../../UserQueries/UserQueryClient'
import { DynamicTypeConditionSymbolEntity } from '../../Dynamic/Signum.Entities.Dynamic'
import { liteKey } from '@framework/Signum.Entities'


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

  if (r.entity) {
    if (newWindow)
      window.open(Navigator.navigateRoute(r.entity));
    else
      Navigator.view(r.entity)
        .then(() => onReload?.());
  } else {
    const fo = extractFindOptions(cr, r);
    const promise = cr.drilldowns.length == 0 ? Promise.resolve({ fo: fo, uq: undefined }) :
      SelectorModal.chooseLite(UserQueryEntity, cr.drilldowns.map(mle => mle.element))
        .then(lite => {
          if (!lite)
            return;

          return Navigator.API.fetch(lite)
            .then(uq => UserQueryClient.Converter.toFindOptions(uq, undefined))
            .then(dfo => {
              dfo.filterOptions = (dfo.filterOptions ?? []).concat(fo.filterOptions);
              dfo.columnOptions = (dfo.columnOptions ?? []).concat(fo.columnOptions);
              dfo.columnOptionsMode = "ReplaceAll";

              return ({ fo: dfo, uq: lite });
            });
        });

    promise.then(val => {
      if (!val)
        return;

      if (newWindow)
        window.open(Finder.findOptionsPath(val.fo, val.uq && { userQuery: liteKey(val.uq) }));
      else
        Finder.explore(val.fo, val.uq && { searchControlProps: { extraOptions: { userQuery: val.uq } } })
          .then(() => onReload && onReload());
    });
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
