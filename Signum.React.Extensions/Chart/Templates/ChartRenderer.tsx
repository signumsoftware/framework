import * as React from 'react'
import { DomUtils, Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { parseLite, is } from '@framework/Signum.Entities'
import { FilterOptionParsed, ColumnOption, hasAggregate, withoutAggregateAndPinned } from '@framework/FindOptions'
import { ChartRequestModel } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import { toFilterOptions } from '@framework/Finder';

import "../Chart.css"
import { ChartScript, chartScripts, ChartRow } from '../ChartClient';
import { ErrorBoundary } from '@framework/Components';

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import ReactChart from '../D3Scripts/Components/ReactChart';


export interface ChartRendererProps {
  chartRequest: ChartRequestModel;
  loading: boolean;
  data?: ChartClient.ChartTable;
  lastChartRequest?: ChartRequestModel;
}

export interface ChartRendererState {
  chartScript?: ChartScript;
  parameters?: { [name: string]: string };
  chartComponent?: (React.ComponentClass<ChartClient.ChartComponentProps>) | ((p: ChartClient.ChartScriptProps) => React.ReactNode);
}


export default class ChartRenderer extends React.Component<ChartRendererProps, ChartRendererState> {

  constructor(props: ChartRendererProps) {
    super(props);
    this.state = {};
  }

  componentWillMount() {
    this.requestAndRedraw(this.props).done();
  }

  componentWillReceiveProps(newProps: ChartRendererProps) {
    if (this.state.chartScript == null || !is(this.state.chartScript.symbol, newProps.chartRequest.chartScript))
      this.requestAndRedraw(newProps).done();
    else {
      if (this.state.chartScript) {
        var newParams = ChartClient.API.getParameterWithDefault(newProps.chartRequest, this.state.chartScript);
        var cleanParams = this.state.parameters && Dic.except(this.state.parameters, Dic.getKeys(this.state.parameters).filter(a => a.startsWith("_")));
        if (!Dic.equals(cleanParams, newParams, false))
          this.setState({ parameters: newParams, });
      }
    }
  }

  async requestAndRedraw(newProps: ChartRendererProps) {

    const chartScriptPromise = ChartClient.getChartScript(newProps.chartRequest.chartScript);
    const chartComponentModulePromise = ChartClient.getRegisteredChartScriptComponent(newProps.chartRequest.chartScript);

    const chartScript = await chartScriptPromise;
    const chartComponentModule = await chartComponentModulePromise();

    const parameters = ChartClient.API.getParameterWithDefault(newProps.chartRequest, chartScript);

    this.setState({ chartComponent: chartComponentModule.default, chartScript, parameters });
  }


  handleDrillDown = (r: ChartRow) => {

    const cr = this.props.lastChartRequest!;

    if (r.entity) {
      window.open(Navigator.navigateRoute(r.entity!));
    } else {
      const filters = cr.filterOptions.map(f => withoutAggregateAndPinned(f)!).filter(Boolean);

      const columns: ColumnOption[] = [];

      cr.columns.map((a, i) => {

        const t = a.element.token;

        if (t && t.token && !hasAggregate(t!.token!) && r.hasOwnProperty("c" + i)) {
          filters.push({
            token: t!.token!,
            operation: "EqualTo",
            value: (r as any)["c" + i],
            frozen: false
          } as FilterOptionParsed);
        }

        if (t && t.token && t.token.parent != undefined) //Avoid Count and simple Columns that are already added
        {
          var col = t.token.queryTokenType == "Aggregate" ? t.token.parent : t.token

          if (col.parent)
            columns.push({
              token: col.fullKey
            });
        }
      });

      window.open(Finder.findOptionsPath({
        queryName: cr.queryKey,
        filterOptions: toFilterOptions(filters),
        columnOptions: columns,
      }));
    }
  }

  render() {
    return (
      <FullscreenComponent>
        <ErrorBoundary>
          {this.state.chartComponent && this.state.parameters &&
            (this.state.chartComponent.prototype instanceof React.Component ?
              React.createElement(this.state.chartComponent as React.ComponentClass<ChartClient.ChartComponentProps>, {
                data: this.props.data,
                loading: this.props.loading,
                onDrillDown: this.handleDrillDown,
                parameters: this.state.parameters
              }) :
              <ReactChart data={this.props.data}
                loading={this.props.loading}
                onDrillDown={this.handleDrillDown}
                parameters={this.state.parameters}
                onRenderChart={this.state.chartComponent as ((p: ChartClient.ChartScriptProps) => React.ReactNode)} />)
      }
        </ErrorBoundary>
      </FullscreenComponent>
    );
  }
}


interface FullscreenComponentProps {

}

interface FullscreenComponentState {
  isFullScreen?: boolean;
}

export class FullscreenComponent extends React.Component<FullscreenComponentProps, FullscreenComponentState> {

  constructor(props: FullscreenComponentProps) {
    super(props);
    this.state = {};
  }

  componentWillMount() {
    this.loadData(this.props);
  }

  componentWillReceiveProps(newProps: FullscreenComponentProps) {
  }

  loadData(props: FullscreenComponentProps) {
  }

  handleExpandToggle = (e: React.MouseEvent<any>) => {
    e.preventDefault();
    this.setState({ isFullScreen: !this.state.isFullScreen });
  }

  render() {
    return (
      <div style={!this.state.isFullScreen ? { display: "flex" } : ({
        display: "flex",
        position: "fixed",
        background: "white",
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        height: "auto",
        zIndex: 9,
      })}>
        <a onClick={this.handleExpandToggle} style={{ color: "gray", order: 2, cursor: "pointer" }} >
          <FontAwesomeIcon icon={this.state.isFullScreen ? "compress" : "expand"} />
        </a>
        <div key={this.state.isFullScreen ? "A" : "B"} style={{ width: "100%", display: "flex" }}> 
          {this.props.children}
        </div>
      </div>
    );
  }
}

