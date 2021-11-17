import * as React from 'react'
import { ServiceError } from '@framework/Services'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import { Entity, Lite, is, JavascriptMessage } from '@framework/Signum.Entities'
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
import { DashboardFilter, DashboardFilterController, DashboardFilterRow, equalsDFR } from "./DashboardFilterController"
import { filterOperations, isFilterGroupOptionParsed } from '@framework/FindOptions'

export default function UserChartPart(p: PanelPartContentProps<UserChartPartEntity>) {

  const qd = useAPI(() => Finder.getQueryDescription(p.part.userChart.query.key), [p.part.userChart.query.key]);
  const chartRequest = useAPI(() => UserChartClient.Converter.toChartRequest(p.part.userChart, p.entity), [p.part.userChart, p.entity, ...p.deps ?? []]);
  const dbFop = React.useMemo(() => chartRequest?.filterOptions.singleOrNull(a => a.pinned?.active == "DashboardFilter"), [chartRequest]);
  const originalFilters = React.useMemo(() => chartRequest?.filterOptions.filter(a => a.pinned == null || a.pinned.active != "DashboardFilter"), [chartRequest]);

  if (chartRequest != null) {
    chartRequest.filterOptions.clear();
    chartRequest.filterOptions = [
      ...originalFilters!,
      ...p.filterController.getFilterOptions(p.partEmbedded, chartRequest!.queryKey),
    ];
  }

  React.useEffect(() => {
    if (dbFop) {

      if (isFilterGroupOptionParsed(dbFop))
        throw new Error("DashboardFilter is not compatible with groups");

      var dashboarFilter = new DashboardFilter(p.partEmbedded, chartRequest!.queryKey);
      if (dbFop.operation == "EqualTo")
        dashboarFilter.rows.push({ filters: [{ token: dbFop.token!, value: dbFop.value }] });
      else if (dbFop.operation == "IsIn") {
        (dbFop.value as any[]).forEach(val => dashboarFilter.rows.push({ filters: [{ token: dbFop!.token!, value: val }] }));
      } else
        throw new Error("DashboardFilter is not compatible with filter operation " + dbFop.operation);
      p.filterController.setFilter(dashboarFilter)
    }
  }, [dbFop]);

  const [resultOrError, makeQuery] = useAPIWithReload<undefined | { error?: any, result?: ChartClient.API.ExecuteChartResult }>(() => chartRequest == null ? Promise.resolve(undefined) :
    ChartClient.getChartScript(chartRequest!.chartScript)
      .then(cs => ChartClient.API.executeChart(chartRequest!, cs))
      .then(result => ({ result }))
      .catch(error => ({ error })),
    [chartRequest && ChartClient.Encoder.chartPath(ChartClient.Encoder.toChartOptions(chartRequest, null)), ...p.deps ?? []], { avoidReset: true });

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
    makeQuery();
  }

  const typeInfos = qd && getTypeInfos(qd.columns["Entity"].type).filter(ti => Navigator.isCreable(ti, { isSearch: true }));
  const handleOnCreateNew = p.part.createNew && typeInfos && typeInfos.length > 0 ? handleCreateNew : undefined;

  function handleCreateNew(e: React.MouseEvent<any>) {
    e.preventDefault();

    return SelectorModal.chooseType(typeInfos!)
      .then(ti => ti && Finder.getPropsFromFilters(ti, chartRequest!.filterOptions)
        .then(props => Constructor.constructPack(ti.name, props)))
      .then(pack => pack && Navigator.view(pack))
      .then(() => makeQuery())
      .done();
  }

  return (
    <div>
      <PinnedFilterBuilder filterOptions={chartRequest.filterOptions} onFiltersChanged={() => makeQuery()} extraSmall={true} />
      {p.part.allowChangeShowData &&
        <label>
          <input type="checkbox" checked={showData} onChange={e => setShowData(e.currentTarget.checked)} />
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
            onOrderChanged={() => makeQuery()}
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
              p.filterController.clear(p.partEmbedded);
            }
          }}
          dashboardFilter={p.filterController.filters.get(p.partEmbedded)}
          onDrillDown={(row, e) => {
            e.stopPropagation();
            if (e.altKey || p.partEmbedded.interactionGroup == null)
              handleDrillDown(row, e, chartRequest, handleReload);
            else {
              const dashboardFilter = p.filterController.filters.get(p.partEmbedded);
              const filterRow = toDashboardFilterRow(row, chartRequest);

              if (e.ctrlKey) {
                const already = dashboardFilter?.rows.firstOrNull(fr => equalsDFR(fr, filterRow));
                if (already) {
                  dashboardFilter!.rows.remove(already);
                  if (dashboardFilter!.rows.length == 0)
                    p.filterController.filters.delete(dashboardFilter!.partEmbedded);
                  else
                    p.filterController.setFilter(dashboardFilter!);
                }
                else {
                  const db = dashboardFilter ?? new DashboardFilter(p.partEmbedded, chartRequest.queryKey);
                  db.rows.push(filterRow);
                  p.filterController.setFilter(db);
                }
              } else {
                const db = new DashboardFilter(p.partEmbedded, chartRequest.queryKey);
                db.rows.push(filterRow);
                p.filterController.setFilter(db);
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

