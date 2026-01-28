import * as React from 'react'
import { ServiceError } from '@framework/Services'
import { JavascriptMessage, toLite, liteKey } from '@framework/Signum.Entities'
import { UserChartClient } from '../../UserChart/UserChartClient'
import { ChartRequestModel } from '../../Signum.Chart'
import ChartTableComponent from '../../Templates/ChartTable'
import PinnedFilterBuilder from '@framework/SearchControl/PinnedFilterBuilder';
import { useForceUpdate } from '@framework/Hooks'
import { MemoRepository } from '../../D3Scripts/Components/ReactChart'
import { CombinedUserChartPartEntity, UserChartEntity } from '../../UserChart/Signum.Chart.UserChart'
import { DashboardClient, PanelPartContentProps } from '../../../Signum.Dashboard/DashboardClient'
import { ChartClient } from '../../ChartClient'
import ChartRendererCombined from '../../Templates/ChartRendererCombined'


export interface CombinedUserChartInfoTemp {
  userChart: UserChartEntity;
  chartScript?: ChartClient.ChartScript;
  parameters?: { [parameter: string]: string } | undefined;
  chartRequest?: ChartRequestModel;
  memo: MemoRepository;
  result?: ChartClient.API.ExecuteChartResult;
  makeQuery?: () => Promise<void>;
  error?: any;
}

export default function CombinedUserChartPart(p: PanelPartContentProps<CombinedUserChartPartEntity>): React.JSX.Element {

  const forceUpdate = useForceUpdate();

  const infos = React.useMemo<CombinedUserChartInfoTemp[]>(() => p.content.userCharts.map(uc => ({ userChart: uc.element.userChart } as CombinedUserChartInfoTemp)), [p.content]);

  const [showData, setShowData] = React.useState(p.content.showData);

  

  React.useEffect(() => {
    var abortController = new AbortController();
    const signal = abortController.signal;

    infos.forEach(c => {

      UserChartClient.Converter.toChartRequest(c.userChart, p.entity)
        .then(chartRequest => {
          c.chartRequest = chartRequest;
          var originalFilters = chartRequest.filterOptions.length;
          c.memo = new MemoRepository();
          forceUpdate();
          if (!signal.aborted) {

            ChartClient.getChartScript(c.chartRequest.chartScript)
              .then(cr => {
                c.chartScript = cr;
                forceUpdate();

                c.makeQuery = () => {

                  if (chartRequest != null) {
                    chartRequest.filterOptions.splice(originalFilters);
                    chartRequest.filterOptions.push(
                      ...p.dashboardController.getFilterOptions(p.partEmbedded, chartRequest!.queryKey),
                    );
                  }

                  var cachedQuery = p.cachedQueries[liteKey(toLite(c.userChart))];

                  if (cachedQuery)
                    return ChartClient.getChartScript(chartRequest!.chartScript)
                      .then(cs => cachedQuery.then(cq => ChartClient.executeChartCached(chartRequest, cs, cq)))
                      .then(result => {
                        if (!signal.aborted) {
                          c.result = result;
                          forceUpdate();
                        }
                      })
                      .catch(error => {
                        if (!signal.aborted) {
                          c.error = error;
                          forceUpdate();
                        }
                      });

                  return ChartClient.API.executeChart(chartRequest!, c.chartScript!, signal)
                    .then(result => {
                      if (!signal.aborted) {
                        c.result = result;
                        forceUpdate();
                      }
                    })
                    .catch(error => {
                      if (!signal.aborted) {
                        c.error = error;
                        forceUpdate();
                      }
                    });
                };

                return c.makeQuery();
              });
          }
        });
    });

    return () => {
      abortController.abort();
    };

  }, [p.content, ...p.deps ?? []]);

  React.useEffect(() => {
    infos.forEach(inf => {
      inf.makeQuery?.();
    });
  }, [p.content, ...p.deps ?? [], infos.max(e => p.dashboardController.getLastChange(e.userChart.query.key))]);

  function renderError(e: any, key: number) {
    const se = e instanceof ServiceError ? (e as ServiceError) : undefined;

    if (se == undefined)
      return <p className="text-danger" key={key}> {e.message ? e.message : e}</p>;

    return (
      <div>
        {se.httpError.exceptionMessage && <p className="text-danger" key={key}>{se.httpError.exceptionMessage}</p>}
      </div>
    );

  }

  if (infos.some(a => a.chartRequest == null || a.chartScript == null))
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  if (infos.some(a => a.error != null)) {
    return (
      <div>
        <h1 className="h4">Error!</h1>
        {
          infos
            .filter(m => m.error != null)
            .map((m, i) => renderError(m.error, i))
        }
      </div>
    );
  }

  return (
    <div>
      {infos.map((info, i) => <PinnedFilterBuilder key={i}
        filterOptions={info.chartRequest!.filterOptions}
        pinnedFilterVisible={fop => fop.dashboardBehaviour == null}
        onFiltersChanged={(fpo, avoidSearch) => !avoidSearch && info.makeQuery!()} extraSmall={true} />
      )}
      {p.content.allowChangeShowData &&
        <label>
          <input type="checkbox" className="form-check-input" checked={showData} onChange={e => setShowData(e.currentTarget.checked)} />
        {" "}{CombinedUserChartPartEntity.nicePropertyName(a => a.showData)}
        </label>}
      {showData ?
        infos.map((c, i) => c.result == null ? <span key={i}>{JavascriptMessage.loading.niceToString()}</span> :
          <ChartTableComponent
            chartRequest={c.chartRequest!}
            lastChartRequest={c.chartRequest!}
            resultTable={c.result.resultTable!}
            onOrderChanged={() => c.makeQuery!()}
            onReload={e => { c.makeQuery!(); }}
          />) :
        <ChartRendererCombined
          infos={infos.map(c => ({ userChart: toLite(c.userChart, true), chartRequest: c.chartRequest!, data: c.result?.chartTable, chartScript: c.chartScript!, memo: c.memo }))}
          onReload={e => { infos.forEach(a => a.makeQuery!()) }}
          useSameScale={p.content.useSameScale}
          minHeigh={p.content.minHeight}
        />
      }
    </div>
  );
}

