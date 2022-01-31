import * as React from 'react'
import { ServiceError } from '@framework/Services'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import { Entity, Lite, is, JavascriptMessage, liteKey, toLite } from '@framework/Signum.Entities'
import * as UserChartClient from '../../Chart/UserChart/UserChartClient'
import * as ChartClient from '../../Chart/ChartClient'
import { ChartMessage, ChartRequestModel } from '../../Chart/Signum.Entities.Chart'
import ChartRenderer, { handleDrillDown } from '../../Chart/Templates/ChartRenderer'
import ChartTableComponent from '../../Chart/Templates/ChartTable'
import { UserChartPartEntity } from '../Signum.Entities.Dashboard'
import PinnedFilterBuilder from '@framework/SearchControl/PinnedFilterBuilder';
import { useAPI, useAPIWithReload } from '@framework/Hooks'
import { PanelPartContentProps } from '../DashboardClient'
import { getTypeInfos } from '@framework/Reflection'
import SelectorModal from '@framework/SelectorModal'
import { DashboardFilter, DashboardController, DashboardFilterRow, DashboardPinnedFilters, equalsDFR } from "./DashboardFilterController"
import { filterOperations, FilterOptionParsed, isFilterGroupOption, isFilterGroupOptionParsed, QueryToken } from '@framework/FindOptions'
import { CachedQueryJS, executeChartCached } from '../CachedQueryExecutor'
import { DashboardBehaviour } from '../../../Signum.React/Scripts/Signum.Entities.DynamicQuery'

export default function UserChartPart(p: PanelPartContentProps<UserChartPartEntity>) {

  const qd = useAPI(() => Finder.getQueryDescription(p.part.userChart.query.key), [p.part.userChart.query.key]);
  const chartRequest = useAPI(() => UserChartClient.Converter.toChartRequest(p.part.userChart, p.entity), [p.part.userChart, p.entity && liteKey(p.entity), ...p.deps ?? []]);
  const initialSelection = React.useMemo(() => chartRequest?.filterOptions.singleOrNull(a => a.dashboardBehaviour == "UseAsInitialSelection"), [chartRequest]);
  const dashboardPinnedFilters = React.useMemo(() => chartRequest?.filterOptions.filter(a => a.dashboardBehaviour == "PromoteToDasboardPinnedFilter"), [chartRequest]);
  const useWhenNoFilters = React.useMemo(() => chartRequest?.filterOptions.filter(a => a.dashboardBehaviour == "UseWhenNoFilters"), [chartRequest]);
  const simpleFilters = React.useMemo(() => chartRequest?.filterOptions.filter(a => a.dashboardBehaviour == null), [chartRequest]);

  if (chartRequest != null) {
    chartRequest.filterOptions.clear();

    var dashboardFilters = p.dashboardController.getFilterOptions(p.partEmbedded, chartRequest!.queryKey);

    function allTokens(fs: FilterOptionParsed[]): QueryToken[] {
      return fs.flatMap(f => isFilterGroupOptionParsed(f) ? [f.token, ...allTokens(f.filters)].notNull() : [f.token].notNull())
    }

    var tokens = allTokens(dashboardFilters);

    chartRequest.filterOptions = [
      ...simpleFilters!,
      ...useWhenNoFilters!.filter(a => !tokens.some(t => t.fullKey == a.token?.fullKey)),
      ...dashboardFilters,
    ];
  }

  React.useEffect(() => {
    if (initialSelection) {

      if (isFilterGroupOptionParsed(initialSelection))
        throw new Error(DashboardBehaviour.niceToString("UseAsInitialSelection") + " is not compatible with groups");

      var dashboarFilter = new DashboardFilter(p.partEmbedded, chartRequest!.queryKey);
      if (initialSelection.operation == "EqualTo")
        dashboarFilter.rows.push({ filters: [{ token: initialSelection.token!, value: initialSelection.value }] });
      else if (initialSelection.operation == "IsIn") {
        (initialSelection.value as any[]).forEach(val => dashboarFilter.rows.push({ filters: [{ token: initialSelection!.token!, value: val }] }));
      } else
        throw new Error("DashboardFilter is not compatible with filter operation " + initialSelection.operation);
      p.dashboardController.setFilter(dashboarFilter)
    } else {
      p.dashboardController.clearFilters(p.partEmbedded);
    }

    if (dashboardPinnedFilters) {
      p.dashboardController.setPinnedFilter(new DashboardPinnedFilters(p.partEmbedded, chartRequest!.queryKey, dashboardPinnedFilters));
    } else {
      p.dashboardController.clearFilters(p.partEmbedded);
    }
  }, [initialSelection, dashboardPinnedFilters]);

  
  const cachedQuery = p.cachedQueries[liteKey(toLite(p.part.userChart))];

  const [resultOrError, reloadQuery] = useAPIWithReload<undefined | { error?: any, result?: ChartClient.API.ExecuteChartResult }>(() => {
    if (chartRequest == null)
      return Promise.resolve(undefined);

    if (cachedQuery)
      return ChartClient.getChartScript(chartRequest!.chartScript)
        .then(cs => cachedQuery.then(cq => executeChartCached(chartRequest, cs, cq)))
        .then(result => ({ result }), error => ({ error }));

    return ChartClient.getChartScript(chartRequest!.chartScript)
      .then(cs => ChartClient.API.executeChart(chartRequest!, cs))
      .then(result => ({ result }), error => ({ error }));

  }, [chartRequest && ChartClient.Encoder.chartPath(ChartClient.Encoder.toChartOptions(chartRequest, null)), ...p.deps ?? []], { avoidReset: true });

  const [showData, setShowData] = React.useState(p.part.showData);
  
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

  if (resultOrError?.error) {
    return (
      <div>
        <h4>Error!</h4>
        {renderError(resultOrError.error)}
      </div>
    );
  }

  const result = resultOrError?.result;

  function handleReload(e?: React.MouseEvent<any>) {
    e?.preventDefault();
    reloadQuery();
  }

  const typeInfos = qd && getTypeInfos(qd.columns["Entity"].type).filter(ti => Navigator.isCreable(ti, { isSearch: true }));
  const handleOnCreateNew = p.part.createNew && typeInfos && typeInfos.length > 0 ? handleCreateNew : undefined;

  function handleCreateNew(e: React.MouseEvent<any>) {
    e.preventDefault();

    return SelectorModal.chooseType(typeInfos!)
      .then(ti => ti && Finder.getPropsFromFilters(ti, chartRequest!.filterOptions)
        .then(props => Constructor.constructPack(ti.name, props)))
      .then(pack => pack && Navigator.view(pack))
      .then(() => reloadQuery())
      .done();
  }

  return (
    <div>
      <PinnedFilterBuilder filterOptions={chartRequest.filterOptions} onFiltersChanged={() => reloadQuery()} pinnedFilterVisible={fop => fop.dashboardBehaviour == null} extraSmall={true} />
      {p.part.allowChangeShowData &&
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
            typeInfos={typeInfos}
            onCreateNew={handleOnCreateNew}
          />) :
        <ChartRenderer
          chartRequest={chartRequest}
          lastChartRequest={chartRequest}
          data={result?.chartTable}
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
              handleDrillDown(row, e, chartRequest, handleReload);
            else {
              const dashboardFilter = p.dashboardController.filters.get(p.partEmbedded);
              const filterRow = toDashboardFilterRow(row, chartRequest);

              if (e.ctrlKey) {
                const already = dashboardFilter?.rows.firstOrNull(fr => equalsDFR(fr, filterRow));
                if (already) {
                  dashboardFilter!.rows.remove(already);
                  if (dashboardFilter!.rows.length == 0)
                    p.dashboardController.filters.delete(dashboardFilter!.partEmbedded);
                  else
                    p.dashboardController.setFilter(dashboardFilter!);
                }
                else {
                  const db = dashboardFilter ?? new DashboardFilter(p.partEmbedded, chartRequest.queryKey);
                  db.rows.push(filterRow);
                  p.dashboardController.setFilter(db);
                }
              } else {
                const db = new DashboardFilter(p.partEmbedded, chartRequest.queryKey);
                db.rows.push(filterRow);
                p.dashboardController.setFilter(db);
              }
            }
          }}
          onReload={handleReload}
          autoRefresh={p.part.autoRefresh}
          typeInfos={typeInfos}
          onCreateNew={handleOnCreateNew}
        />
      }
    </div>
  );
}


function toDashboardFilterRow(row: ChartClient.ChartRow, chartRequest: ChartRequestModel): DashboardFilterRow {
  var filters = chartRequest.columns.map((c, i) => ({
    token: c.element.token?.token,
    value: (row as any)["c" + i],
  })).filter(a => a.token != null && a.token.queryTokenType != "Aggregate" && a.value !== undefined);

  return { filters: filters } as DashboardFilterRow;
}

