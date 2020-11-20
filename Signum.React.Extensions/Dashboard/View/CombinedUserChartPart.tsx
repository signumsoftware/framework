
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
import { useAPI, useAPIWithReload, useForceUpdate, useSize, useThrottle } from '@framework/Hooks'
import { PanelPartContentProps } from '../DashboardClient'
import { getTypeInfos } from '@framework/Reflection'
import SelectorModal from '@framework/SelectorModal'
import { QueryDescription } from '@framework/FindOptions'
import { ErrorBoundary } from '@framework/Components'
import { FullscreenComponent } from '../../Chart/Templates/FullscreenComponent'
import { classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import ReactChart from '../../Chart/D3Scripts/Components/ReactChart'

interface CombinedUserChartInfo {
  userChart: UserChartEntity;
  qd?: QueryDescription;
  chartRequest?: ChartRequestModel;
  error?: any;
  result?: ChartClient.API.ExecuteChartResult;
}

export default function CombinedUserChartPart(p: PanelPartContentProps<CombinedUserChartPartEntity>) {

  const infos = React.useRef<CombinedUserChartInfo[]>([]);
  const forceUpdate = useForceUpdate();

  const [invalidate, setInvalidate] = React.useState<number>(0)

  React.useEffect(() => {

    infos.current = p.part.userCharts.map(uc => ({ userChart: uc.element } as CombinedUserChartInfo));

    var abortController = new AbortController();

    infos.current.forEach(c => {

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

  if (!infos.current.every(a => a.chartRequest != null))
    return <span>{JavascriptMessage.loading.niceToString()}</span>;

  if (infos.current.some(a => a.error != null)) {
    return (
      <div>
        <h4>Error!</h4>
        {
          infos.current
            .filter(m => m.error != null)
            .map((m, i) => renderError(m.error, i))
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
        <ErrorBoundary refreshKey={infos.current}>
          {cs && parameters &&
            <CombinedReactChart
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


export function CombinedReactChart(p: { infos: CombinedUserChartInfo[] }) {

  const isSimple = p.infos.every(a => a.result == null || a.result.resultTable.rows.length < ReactChart.maxRowsForAnimation);
  const allData = p.infos.every(a => a.result != null);
  const oldAllData = useThrottle(allData, 200, { enabled: isSimple });
  const initialLoad = oldAllData == false && allData && isSimple;

  const { size, setContainer } = useSize();

  return (
    <div className={classes("sf-chart-container", isSimple ? "sf-chart-animable" : "")} ref={setContainer} >
      {size &&
        p.onRenderChart({
          chartRequest: p.chartRequest,
          data: p.data,
          parameters: p.parameters,
          loading: p.loading,
          onDrillDown: p.onDrillDown,
          height: size.height,
          width: size.width,
          initialLoad: initialLoad,
        })
      }
    </div>
  );
}
