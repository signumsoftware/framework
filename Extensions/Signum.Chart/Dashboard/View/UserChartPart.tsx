import * as React from 'react'
import { ServiceError } from '@framework/Services'
import { JavascriptMessage, liteKey, toLite } from '@framework/Signum.Entities'
import { UserChartClient } from '../../UserChart/UserChartClient'
import { ChartClient, ChartRow } from '../../ChartClient'
import { ChartMessage, ChartRequestModel } from '../../Signum.Chart'
import ChartRenderer, { handleDrillDown } from '../../Templates/ChartRenderer'
import ChartTableComponent from '../../Templates/ChartTable'
import PinnedFilterBuilder from '@framework/SearchControl/PinnedFilterBuilder';
import { useAPI, useAPIWithReload } from '@framework/Hooks'
import { FilterOptionParsed, isActive, isFilterGroup, QueryToken, tokenStartsWith } from '@framework/FindOptions'
import { DashboardBehaviour } from '@framework/Signum.DynamicQuery'
import { softCast } from '@framework/Globals'
import { DashboardClient, PanelPartContentProps } from '../../../Signum.Dashboard/DashboardClient'
import { UserChartPartEntity } from '../../UserChart/Signum.Chart.UserChart'
import { DashboardFilter, DashboardFilterRow, DashboardPinnedFilters, equalsDFR } from '../../../Signum.Dashboard/View/DashboardFilterController'

export interface UserChartPartHandler {
  chartRequest: ChartRequestModel | undefined;
  reloadQuery: () => void;
}

export default function UserChartPart(p: PanelPartContentProps<UserChartPartEntity>): React.JSX.Element {

  const chartRequest = useAPI(() => UserChartClient.Converter.toChartRequest(p.content.userChart, p.entity), [p.content.userChart, p.entity && liteKey(p.entity), ...p.deps ?? []]);
  const initialSelection = React.useMemo(() => chartRequest?.filterOptions.singleOrNull(a => a.dashboardBehaviour == "UseAsInitialSelection"), [chartRequest]);
  const dashboardPinnedFilters = React.useMemo(() => chartRequest?.filterOptions.filter(a => a.dashboardBehaviour == "PromoteToDasboardPinnedFilter"), [chartRequest]);
  const useWhenNoFilters = React.useMemo(() => chartRequest?.filterOptions.filter(a => a.dashboardBehaviour == "UseWhenNoFilters"), [chartRequest]);
  const simpleFilters = React.useMemo(() => chartRequest?.filterOptions.filter(a => a.dashboardBehaviour == null), [chartRequest]);
  const [refreshKey, setRefreshKey] = React.useState<number>(0);


  if (chartRequest != null) {
    chartRequest.filterOptions.clear();

    var dashboardFilters = p.dashboardController.getFilterOptions(p.partEmbedded, chartRequest!.queryKey);

    function allTokens(fs: FilterOptionParsed[]): QueryToken[] {
      return fs.flatMap(f => isFilterGroup(f) ? [f.token, ...allTokens(f.filters)].notNull() : [f.token].notNull())
    }

    var tokens = allTokens(dashboardFilters.filter(df => isActive(df)));

    chartRequest.filterOptions = [
      ...simpleFilters!,
      ...useWhenNoFilters!.filter(a => !tokens.some(t => tokenStartsWith(a.token!, t))),
      ...dashboardFilters,
    ];
  }

  React.useEffect(() => {

    if (initialSelection) {

      if (isFilterGroup(initialSelection))
        throw new Error(DashboardBehaviour.niceToString("UseAsInitialSelection") + " is not compatible with groups");

      var dashboarFilter = new DashboardFilter(p.partEmbedded, chartRequest!.queryKey);
      if (initialSelection.operation == "EqualTo")
        dashboarFilter.rows.push({ filters: [{ token: initialSelection.token!, value: initialSelection.value }] });
      else if (initialSelection.operation == "IsIn") {
        (initialSelection.value as any[]).forEach(val => dashboarFilter.rows.push({ filters: [{ token: initialSelection!.token!, value: val }] }));
      } else
        throw new Error("DashboardFilter is not compatible with filter operation " + initialSelection.operation);

      p.dashboardController.setFilter(dashboarFilter);


    } else {
      p.dashboardController.clearFilters(p.partEmbedded);
    }

    if (dashboardPinnedFilters) {
      p.dashboardController.setPinnedFilter(new DashboardPinnedFilters(p.partEmbedded, chartRequest!.queryKey, dashboardPinnedFilters));
    } else {
      p.dashboardController.clearPinnesFilter(p.partEmbedded);
    }

    if (chartRequest) {
      p.dashboardController.registerInvalidations(p.partEmbedded, () => setRefreshKey(a => a + 1));
    }

  }, [chartRequest]);

  const cachedQuery = p.cachedQueries[liteKey(toLite(p.content.userChart))];

  const [resultOrError, reloadQuery] = useAPIWithReload<undefined | { error?: any, result?: ChartClient.API.ExecuteChartResult }>(() => {
    if (chartRequest == null || p.dashboardController.isLoading)
      return Promise.resolve(undefined);

    if (cachedQuery)
      return ChartClient.getChartScript(chartRequest!.chartScript)
        .then(cs => cachedQuery.then(cq => ChartClient.executeChartCached(chartRequest, cs, cq)))
        .then(result => ({ result }), error => ({ error }));

    return ChartClient.getChartScript(chartRequest!.chartScript)
      .then(cs => ChartClient.API.executeChart(chartRequest!, cs))
      .then(result => ({ result }), error => ({ error }));

  }, [
    chartRequest && ChartClient.Encoder.chartPath(ChartClient.Encoder.toChartOptions(chartRequest, null)),
    p.dashboardController.isLoading,
    ...p.deps ?? []
  ], { avoidReset: true });

  p.customDataRef.current = softCast<UserChartPartHandler>({
    chartRequest,
    reloadQuery
  });

  React.useEffect(() => {
    p.dashboardController.registerInvalidations(p.partEmbedded, () => setRefreshKey(a => a + 1));
  }, [p.partEmbedded])

  const [showData, setShowData] = React.useState(p.content.showData);
  
  function renderError(e: any) {
    const se = e instanceof ServiceError ? (e as ServiceError) : undefined;

    if (se == undefined)
      return <p className="text-danger"> {e.message ? e.message : e}</p>;

    return (
      <div>
        {se.httpError.exceptionMessage && <p className="text-danger">{se.httpError.exceptionMessage}</p>}
      </div>
    );
  }

  if (!chartRequest)
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  if (p.dashboardController.isLoading)
    return <span>{JavascriptMessage.loading.niceToString()}...</span>;

  if (resultOrError?.error) {
    return (
      <div>
        <h4>Error!</h4>
        {renderError(resultOrError.error)}
      </div>
    );
  }

  function handleReload(e?: React.MouseEvent<any>) {
    reloadQuery();
  }

  const result = resultOrError?.result;
  const userChart = toLite(p.content.userChart, true);

  return (
    <div className="d-flex flex-column flex-grow-1">
      <PinnedFilterBuilder filterOptions={chartRequest.filterOptions} onFiltersChanged={(fops, avoidSearch) => !avoidSearch && reloadQuery()} pinnedFilterVisible={fop => fop.dashboardBehaviour == null} extraSmall={true} />
      {p.content.allowChangeShowData &&
        <label>
          <input type="checkbox" className="form-check-input" checked={showData} onChange={e => setShowData(e.currentTarget.checked)} />
          {" "}{UserChartPartEntity.nicePropertyName(a => a.showData)}
        </label>}
      {result != null && chartRequest.maxRows == result.resultTable.rows.length ?
        <p className="text-danger">{ChartMessage.QueryResultReachedMaxRows0.niceToString(result.resultTable.rows.length)}</p> : undefined}
      {showData ?
        (!result ? <span>{JavascriptMessage.loading.niceToString()}</span> :
          <ChartTableComponent
            chartRequest={chartRequest}
            lastChartRequest={chartRequest}
            resultTable={result.resultTable!}
            onOrderChanged={() => reloadQuery()}
            onReload={handleReload}
          />) :
        <ChartRenderer
          userChart={userChart}
          chartRequest={chartRequest}
          lastChartRequest={chartRequest}
          data={result?.chartTable}
          minHeight={p.content.minHeight}
          loading={result === null}
          onBackgroundClick={e => {
            if (!e.ctrlKey) {
              p.dashboardController.clearFilters(p.partEmbedded);
            }
          }}
          dashboardFilter={p.dashboardController.filters.get(p.partEmbedded)}
          onDrillDown={(row, e) => {
            e.stopPropagation();
            if (e.altKey || p.partEmbedded.interactionGroup == null)
              handleDrillDown(row, e, chartRequest, userChart, handleReload);
            else {
              const dashboardFilter = p.dashboardController.filters.get(p.partEmbedded);
              const filterRow = toDashboardFilterRow(row, chartRequest);

              if (e.ctrlKey) {
                const already = dashboardFilter?.rows.firstOrNull(fr => equalsDFR(fr, filterRow));
                if (already) {
                  dashboardFilter!.rows.remove(already);
                  if (dashboardFilter!.rows.length == 0)
                    p.dashboardController.clearFilters(dashboardFilter!.partEmbedded);
                  else
                    p.dashboardController.setFilter(dashboardFilter!);
                }
                else {
                  const db = dashboardFilter ?? new DashboardFilter(p.partEmbedded, chartRequest.queryKey);
                  db.rows.push(filterRow);
                  p.dashboardController.setFilter(db);
                }
              } else {
                const already = dashboardFilter?.rows.firstOrNull(fr => equalsDFR(fr, filterRow));
                if (already && dashboardFilter?.rows.length == 1) {
                  p.dashboardController.clearFilters(dashboardFilter!.partEmbedded);
                } else {
                  const db = new DashboardFilter(p.partEmbedded, chartRequest.queryKey);
                  db.rows.push(filterRow);
                  p.dashboardController.setFilter(db);
                }
              }
            }
          }}
          onReload={handleReload}
          autoRefresh={p.content.autoRefresh}
        />
      }
    </div>
  );
}


function toDashboardFilterRow(row: ChartRow, chartRequest: ChartRequestModel): DashboardFilterRow {
  var filters = chartRequest.columns.map((c, i) => ({
    token: c.element.token?.token,
    value: (row as any)["c" + i],
  })).filter(a => a.token != null && a.token.queryTokenType != "Aggregate" && a.value !== undefined);

  return { filters: filters } as DashboardFilterRow;
}

