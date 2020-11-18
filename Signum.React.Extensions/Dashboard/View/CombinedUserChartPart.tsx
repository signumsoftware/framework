
import * as React from 'react'
import { ServiceError } from '@framework/Services'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import { Entity, Lite, is, JavascriptMessage } from '@framework/Signum.Entities'
import * as UserChartClient from '../../Chart/UserChart/UserChartClient'
import * as ChartClient from '../../Chart/ChartClient'
import { ChartRequestModel, UserChartEntity } from '../../Chart/Signum.Entities.Chart'
import ChartRenderer from '../../Chart/Templates/ChartRenderer'
import ChartTableComponent from '../../Chart/Templates/ChartTable'
import { CombinedUserChartPartEntity, UserChartPartEntity } from '../Signum.Entities.Dashboard'
import PinnedFilterBuilder from '@framework/SearchControl/PinnedFilterBuilder';
import { useAPI, useAPIWithReload, useForceUpdate } from '@framework/Hooks'
import { PanelPartContentProps } from '../DashboardClient'
import { getTypeInfos } from '@framework/Reflection'
import SelectorModal from '@framework/SelectorModal'
import { QueryDescription } from '@framework/FindOptions'
import { ErrorBoundary } from '@framework/Components'
import { FullscreenComponent } from '../../Chart/Templates/FullscreenComponent'

interface CombinedUserChartInfo {
  userChart: UserChartEntity;
  qd?: QueryDescription;
  chartRequest?: ChartRequestModel;
  error?: any;
  result?: ChartClient.API.ExecuteChartResult;
}

export default function CombinedUserChartPart(p: PanelPartContentProps<CombinedUserChartPartEntity>) {

  const parts = React.useRef<CombinedUserChartInfo[]>([]);
  const forceUpdate = useForceUpdate();

  const [invalidate, setInvalidate] = React.useState<number>(0)

  React.useEffect(() => {

    parts.current = p.part.userCharts.map(uc => ({ userChart: uc.element } as CombinedUserChartInfo));

    var abortController = new AbortController();

    parts.current.forEach(c => {

      Finder.getQueryDescription(c.userChart.query.key)
        .then(qd => {
          c.qd = qd;
          forceUpdate();
        }).done();

      UserChartClient.Converter.toChartRequest(c.userChart, p.entity)
        .then(chartRequest => {
          c.chartRequest = chartRequest;
          forceUpdate();
          if (!abortController.signal.aborted) {
            ChartClient.getChartScript(chartRequest.chartScript)
              .then(cs => ChartClient.API.executeChart(chartRequest!, cs))
              .then(result => {
                c.result = result;
                forceUpdate();
              })
              .catch(error => {
                c.error = error;
                forceUpdate();
              });
          }
        }).done();
    });

    return () => {
      abortController.abort();
    };

  }, [p.part, invalidate]);

 
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

  if (!parts.current.every(a => a.chartRequest != null))
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  if (parts.current.some(a => a.error != null)) {
    return (
      <div>
        <h4>Error!</h4>
        {
          parts.current
            .filter(m => m.error != null)
            .map((m, i) => renderError(m.error))
        }
      </div>
    );
  }

  function handleReload(e: React.MouseEvent<any>) {
    e.preventDefault();
    setInvalidate(a => a + 1);
  }

  return (
    <div>
      <FullscreenComponent onReload={handleReload}>
        <ErrorBoundary refreshKey={parts.current}>
          {cs && parameters &&
            <ReactChart
              chartRequest={p.chartRequest}
              data={p.data}
              loading={p.loading}
              onDrillDown={(r, e) => handleDrillDown(r, e, p.lastChartRequest!)}
              parameters={parameters}
              onRenderChart={cs.chartComponent as ((p: ChartClient.ChartScriptProps) => React.ReactNode)} />
          }
        </ErrorBoundary>
      </FullscreenComponent>
    </div>
  );
}
