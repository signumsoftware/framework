import * as React from 'react'
import { Tab, Tabs } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ifError } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { ValidationError, AbortableRequest } from '@framework/Services'
import { Lite } from '@framework/Signum.Entities'
import { QueryDescription, SubTokensOptions, QueryToken } from '@framework/FindOptions'
import { TypeContext } from '@framework/TypeContext'
import { SearchMessage, JavascriptMessage } from '@framework/Signum.Entities'
import { PropertyRoute, getQueryNiceName, getTypeInfo, ReadonlyBinding, GraphExplorer } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import FilterBuilder from '@framework/SearchControl/FilterBuilder'
import { ValidationErrors } from '@framework/Frames/ValidationErrors'
import { ChartRequestModel, ChartMessage, UserChartEntity } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import ChartBuilder from './ChartBuilder'
import ChartTableComponent from './ChartTable'
import ChartRenderer from './ChartRenderer'
import "@framework/SearchControl/Search.css"
import "../Chart.css"
import { ChartScript } from '../ChartClient';
import { useForceUpdate, useAPI } from '@framework/Hooks'


interface ChartRequestViewProps {
  chartRequest: ChartRequestModel;
  userChart?: Lite<UserChartEntity>;
  onChange: (newChartRequest: ChartRequestModel, userChart?: Lite<UserChartEntity>) => void;
  title?: string;
}

export interface ChartRequestViewHandle {
  chartRequest: ChartRequestModel;
  userChart?: Lite<UserChartEntity>;
  onChange(cr: ChartRequestModel, uc?: Lite<UserChartEntity>): void;
}

export default function ChartRequestView(p: ChartRequestViewProps) {
  const forceUpdate = useForceUpdate();
  const lastToken = React.useRef<QueryToken | undefined>(undefined);

  const [resultAndLoading, setResult] = React.useState<{
    result: {
      chartRequest: ChartRequestModel; //Use to check validity of results
      lastChartRequest: ChartRequestModel; 
      chartResult: ChartClient.API.ExecuteChartResult;
    } | undefined, loading: boolean;
  } | undefined>(undefined);

  const queryDescription = useAPI(signal => p.chartRequest ? Finder.getQueryDescription(p.chartRequest.queryKey) : Promise.resolve(undefined),
    [p.chartRequest.queryKey]);

  const abortableQuery = React.useRef(new AbortableRequest<{ cr: ChartRequestModel; cs: ChartScript }, ChartClient.API.ExecuteChartResult>((signal, request) => ChartClient.API.executeChart(request.cr, request.cs, signal)));
  React.useEffect(() => {
    return () => { abortableQuery.current.abort(); }
  }, []);

  function handleTokenChange() {
    removeObsoleteOrders();
  }

  function handleInvalidate() {
    setResult(undefined);
    forceUpdate();
  }

  function removeObsoleteOrders() {
    var cr = p.chartRequest;
    cr.columns.filter(a => a.element.token == null).forEach(a => {
      a.element.orderByIndex = null;
      a.element.orderByType = null;
    })
  }

  function handleOnRedraw() {
    forceUpdate();
    p.onChange(p.chartRequest, p.userChart);
  }

  function handleOnDrawClick() {
    setResult({ result: resultAndLoading?.result, loading: true });

    var cr = p.chartRequest;

    cr.columns.filter(a => a.element.token == null).forEach(a => {
      a.element.orderByIndex = null;
      a.element.orderByType = null;
    });

    GraphExplorer.setModelState(cr, undefined, "");

    ChartClient.getChartScript(cr.chartScript)
      .then(cs => abortableQuery.current.getData({ cr, cs }))
      .then(rt => {
        setResult({
          result: {
            chartResult: rt,
            chartRequest: cr,
            lastChartRequest: JSON.parse(JSON.stringify(cr))
          }, loading: false
        });
        p.onChange(cr, p.userChart);
      }, ifError(ValidationError, e => {
        GraphExplorer.setModelState(cr, e.modelState, "");
        forceUpdate();
      })).done();
  }

  function handleOnFullScreen(e: React.MouseEvent<any>) {
    e.preventDefault();
    ChartClient.Encoder.chartPathPromise(p.chartRequest)
      .then(path => Navigator.history.push(path))
      .done();
  }

  function handleExplore(e: React.MouseEvent<any>) {
    const cr = p.chartRequest;

    var path = Finder.findOptionsPath({
      queryName: cr.queryKey,
      filterOptions: Finder.toFilterOptions(cr.filterOptions),
    });

    Navigator.pushOrOpenInTab(path, e);
  }
  const qd = queryDescription;
  if (qd == undefined)
    return null;

  const cr = p.chartRequest;
  const tc = new TypeContext<ChartRequestModel>(undefined, undefined, PropertyRoute.root(getTypeInfo(cr.Type)), new ReadonlyBinding(p.chartRequest!, "chartRequest"));

  const loading = resultAndLoading?.loading;
  const result = resultAndLoading?.result && resultAndLoading.result.chartRequest == p.chartRequest ? resultAndLoading.result : undefined;
  return (
    <div>
      <h2>
        <span className="sf-entity-title">{getQueryNiceName(cr.queryKey)}</span>&nbsp;
        <a className="sf-popup-fullscreen" href="#" onClick={handleOnFullScreen}>
          <FontAwesomeIcon icon="external-link-alt" />
        </a>
      </h2 >
      <ValidationErrors entity={cr} prefix="chartRequest" />
      <div className="sf-chart-control sf-control-container" >
        <div>
          <FilterBuilder filterOptions={cr.filterOptions} queryDescription={queryDescription!}
            subTokensOptions={SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement}
            lastToken={lastToken.current} onTokenChanged={t => lastToken.current = t} showPinnedFilters={true} />

        </div>
        <div className="sf-control-container">
          <ChartBuilder queryKey={cr.queryKey} ctx={tc}
            onInvalidate={handleInvalidate}
            onRedraw={handleOnRedraw}
            onTokenChange={handleTokenChange}
            onOrderChanged={() => {
              if (result)
                handleOnDrawClick();
              else
                forceUpdate();
            }}
          />
        </div >
        <div className="sf-query-button-bar btn-toolbar">
          <button type="submit" className="sf-query-button sf-chart-draw btn btn-primary" onClick={handleOnDrawClick}>{ChartMessage.DrawChart.niceToString()}</button>
          {ChartClient.ButtonBarChart.getButtonBarElements({ chartRequest: cr, chartRequestView: { chartRequest: cr, userChart: p.userChart, onChange: p.onChange } }).map((a, i) => React.cloneElement(a, { key: i }))}
          <button className="btn btn-light" onMouseUp={handleExplore} ><FontAwesomeIcon icon="search" /> &nbsp; {SearchMessage.Explore.niceToString()}</button>
        </div>
        <br />
        <div className="sf-scroll-table-container" >
          <Tabs id="chartResultTabs">
            <Tab eventKey="chart" title={ChartMessage.Chart.niceToString()}>
              <ChartRenderer chartRequest={cr} loading={loading == true} lastChartRequest={result?.lastChartRequest} data={result?.chartResult.chartTable} />
            </Tab>
            {result &&
              <Tab eventKey="data" title={<span>{ChartMessage.Data.niceToString()} ({(result.chartResult.resultTable.rows.length)})</span> as any}>
                <ChartTableComponent chartRequest={cr} lastChartRequest={result.lastChartRequest} resultTable={result.chartResult.resultTable}
                  onOrderChanged={() => handleOnDrawClick()} />
              </Tab>
            }
          </Tabs>
        </div>
      </div>
    </div>
  );
}
