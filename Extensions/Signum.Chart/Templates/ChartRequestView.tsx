import * as React from 'react'
import { Tab, Tabs } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { ifError, classes } from '@framework/Globals'
import * as AppContext from '@framework/AppContext'
import { Finder } from '@framework/Finder'
import { ValidationError, AbortableRequest } from '@framework/Services'
import { FrameMessage, Lite } from '@framework/Signum.Entities'
import { SubTokensOptions, QueryToken, FilterOptionParsed } from '@framework/FindOptions'
import { StyleContext, TypeContext } from '@framework/TypeContext'
import { PropertyRoute, getQueryNiceName, getTypeInfo, ReadonlyBinding, GraphExplorer, getTypeInfos } from '@framework/Reflection'
import { Navigator } from '@framework/Navigator'
import FilterBuilder from '@framework/SearchControl/FilterBuilder'
import { ValidationErrors } from '@framework/Frames/ValidationErrors'
import { ChartRequestModel, ChartMessage, ChartTimeSeriesEmbedded } from '../Signum.Chart'
import ChartBuilder from './ChartBuilder'
import ChartTableComponent from './ChartTable'
import ChartRenderer from './ChartRenderer'
import "@framework/SearchControl/Search.css"
import "../Chart.css"
import { ChartClient } from '../ChartClient';
import { useForceUpdate, useAPI } from '@framework/Hooks'
import { AutoFocus } from '@framework/Components/AutoFocus';
import PinnedFilterBuilder from '@framework/SearchControl/PinnedFilterBuilder';
import { UserChartEntity } from '../UserChart/Signum.Chart.UserChart';
import ChartTimeSeries from './ChartTimeSeries';
import { DateTime } from 'luxon';

interface ChartRequestViewProps {
  chartRequest: ChartRequestModel;
  userChart?: Lite<UserChartEntity>;
  onChange: (newChartRequest: ChartRequestModel, userChart?: Lite<UserChartEntity>) => void;
  title?: string;
  showChartSettings?: boolean;
  searchOnLoad?: boolean;
  onFiltersChanged?: (filters: FilterOptionParsed[]) => void;
}

export interface ChartRequestViewHandle {
  chartRequest: ChartRequestModel;
  userChart?: Lite<UserChartEntity>;
  onChange(cr: ChartRequestModel, uc?: Lite<UserChartEntity>): void;
  hideFiltersAndSettings: () => void;
}

export default function ChartRequestView(p: ChartRequestViewProps): React.JSX.Element | null {
  const forceUpdate = useForceUpdate();
  const lastToken = React.useRef<QueryToken | undefined>(undefined);

  const [showChartSettings, setShowChartSettings] = React.useState(p.showChartSettings ?? false);

  const [resultAndLoading, setResult] = React.useState<{
    result: {
      chartRequest: ChartRequestModel; //Use to check validity of results
      lastChartRequest: ChartRequestModel;
      chartResult: ChartClient.API.ExecuteChartResult;
    } | undefined,
    loading: boolean;
  } | undefined>(undefined);

  const queryDescription = useAPI(signal => p.chartRequest ? Finder.getQueryDescription(p.chartRequest.queryKey) : Promise.resolve(undefined),
    [p.chartRequest.queryKey]);

  const abortableQuery = React.useRef(new AbortableRequest<{ cr: ChartRequestModel; cs: ChartClient.ChartScript }, ChartClient.API.ExecuteChartResult>(
    (signal, request) => Navigator.API.validateEntity(ChartClient.cleanedChartRequest(request.cr)).then(() => ChartClient.API.executeChart(request.cr, request.cs, signal))));

  React.useEffect(() => {
    if (p.searchOnLoad)
      handleOnDrawClick();

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
      }));
  }

  function handleFiltersChanged() {
    if (p.onFiltersChanged)
      p.onFiltersChanged(cr.filterOptions);
  }

  function handlePinnedFilterChanged(fop: FilterOptionParsed[], avoidSearch?: boolean) {
    handleFiltersChanged();
    if (!avoidSearch)
      handleOnDrawClick();
  }

  function handleOnFullScreen(e: React.MouseEvent<any>) {
    e.preventDefault();
    ChartClient.Encoder.chartPathPromise(p.chartRequest)
      .then(path => AppContext.navigate(path));
  }

  function handleExplore(e: React.MouseEvent<any>) {
    const cr = p.chartRequest;

    var path = Finder.findOptionsPath({
      queryName: cr.queryKey,
      filterOptions: Finder.toFilterOptions(cr.filterOptions),
    });

    AppContext.pushOrOpenInTab(path, e);
  }


  function handleHideFiltersAndSettings() {
    setShowChartSettings(false);
  }

  const qd = queryDescription;
  if (qd == undefined)
    return null;

  const cr = p.chartRequest;
  const tc = new TypeContext<ChartRequestModel>(undefined, undefined, PropertyRoute.root(getTypeInfo(cr.Type)), new ReadonlyBinding(p.chartRequest!, "chartRequest"));

  const loading = resultAndLoading?.loading;
  const result = resultAndLoading?.result && resultAndLoading.result.chartRequest == p.chartRequest ? resultAndLoading.result : undefined;

  const titleLabels = StyleContext.default.titleLabels;
  const maxRowsReached = result && result.chartRequest.maxRows == result.chartResult.resultTable.rows.length;
  const canTimeSeries = cr.chartTimeSeries != null ? SubTokensOptions.CanTimeSeries : 0;
  return (
    <div style={{ display: "flex", flexDirection: "column", flexGrow: 1 }}>
      <h2>
        <span className="sf-entity-title">{getQueryNiceName(cr.queryKey)}</span>&nbsp;
        <a className="sf-popup-fullscreen" href="#" onClick={handleOnFullScreen} >
          <FontAwesomeIcon icon="up-right-from-square" title={FrameMessage.Fullscreen.niceToString()}/>
        </a>
      </h2 >
      <ValidationErrors entity={cr} prefix="chartRequest" />
      <div>
        {showChartSettings ?
          <FilterBuilder filterOptions={cr.filterOptions} queryDescription={queryDescription!}
            subTokensOptions={SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canTimeSeries}
            onFiltersChanged={handleFiltersChanged}
            lastToken={lastToken.current} onTokenChanged={t => lastToken.current = t} showPinnedFiltersOptionsButton={true} /> :
          <AutoFocus>
            <PinnedFilterBuilder
              filterOptions={cr.filterOptions}
              onFiltersChanged={handlePinnedFilterChanged} />
          </AutoFocus>
        }
      </div>
      <div className="sf-control-container">
        {showChartSettings && <>
          <ChartBuilder queryKey={cr.queryKey} ctx={tc}
            maxRowsReached={maxRowsReached}
            onInvalidate={handleInvalidate}
            queryDescription={qd}
            onRedraw={handleOnRedraw}
            onTokenChange={() => { handleTokenChange(); forceUpdate(); }}
            onOrderChanged={() => {
              if (result)
                handleOnDrawClick();
              else
                forceUpdate();
            }}
          />
        </>}
      </div>
      <div className="sf-query-button-bar btn-toolbar my-2 bg-body rounded shadow-sm p-2">
        <button
          className={classes("sf-query-button btn", showChartSettings && "active", "btn-tertiary")}
          onClick={() => { setShowChartSettings(!showChartSettings); }}
          title={titleLabels ? showChartSettings ? ChartMessage.HideChartSettings.niceToString() : ChartMessage.ShowChartSettings.niceToString() : undefined}>
          <FontAwesomeIcon icon="sliders" />
        </button>
        <button type="submit" className="sf-query-button sf-chart-draw btn btn-primary" onClick={handleOnDrawClick}>{ChartMessage.DrawChart.niceToString()}</button>
        {ChartClient.ButtonBarChart.getButtonBarElements({
          chartRequest: cr,
          chartRequestView: { chartRequest: cr, userChart: p.userChart, onChange: p.onChange, hideFiltersAndSettings: handleHideFiltersAndSettings }
        }).map((a, i) => React.cloneElement(a, { key: i }))}
        <button className="btn btn-tertiary" onMouseUp={handleExplore} ><FontAwesomeIcon icon="magnifying-glass" /> &nbsp; {ChartMessage.ListView.niceToString()}</button>
      </div>
      <div className="sf-chart-tab-container">
        <Tabs id="chartResultTabs" key={showChartSettings + ""}>
          <Tab eventKey="chart" title={ChartMessage.Chart.niceToString()}>
            <ChartRenderer userChart={p.userChart} chartRequest={cr} loading={loading == true} autoRefresh={false} lastChartRequest={result?.lastChartRequest} data={result?.chartResult.chartTable} minHeight={null} />
          </Tab>
          {result &&
            <Tab eventKey="data" title={<span>{ChartMessage.Data.niceToString()} (
            <span
              className={maxRowsReached ? "text-danger fw-bold" : undefined}
              title={maxRowsReached ? ChartMessage.QueryResultReachedMaxRows0.niceToString(result.chartRequest.maxRows) : undefined}>
                {(result.chartResult.resultTable.rows.length)}
              </span>
            )
            </span> as any}>
              <ChartTableComponent chartRequest={cr} lastChartRequest={result.lastChartRequest} resultTable={result.chartResult.resultTable}
                onOrderChanged={() => handleOnDrawClick()} />
            </Tab>
          }
        </Tabs>
      </div>
    </div>
  );
}
