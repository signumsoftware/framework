import * as React from 'react'
import { DomUtils, Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { parseLite, is } from '@framework/Signum.Entities'
import { FilterOptionParsed, ColumnOption, hasAggregate } from '@framework/FindOptions'
import { ChartRequestModel } from '../Signum.Entities.Chart'
import * as ChartClient from '../ChartClient'
import { toFilterOptions } from '@framework/Finder';

import "../Chart.css"
import { ChartScript, chartScripts, ChartRow } from '../ChartClient';
import { ErrorBoundary } from '@framework/Components';


export interface ChartRendererProps {
  data: ChartClient.ChartTable;
  chartRequest: ChartRequestModel;
  lastChartRequest: ChartRequestModel;
}

export interface ChartRendererState {
  chartScript?: ChartScript;
  chartComponent?: React.ComponentClass<{ data: ChartClient.ChartTable, onDrillDown: (e: ChartRow) => void }>;
}


export default class ChartRenderer extends React.Component<ChartRendererProps, ChartRendererState> {

  constructor(props: ChartRendererProps) {
    super(props);
    this.state = {};
  }

  componentWillMount() {
    this.requestAndRedraw().done();
  }

  lastChartRequestPath: string | undefined;
  shouldComponentUpdate(newProps: ChartRendererProps) {
    if (this.props.data != newProps.data)
      return true;

    if (this.lastChartRequestPath != ChartClient.Encoder.chartPath(newProps.chartRequest))
      return true;

    return false;
  }

  componentWillReceiveProps(newProps: ChartRendererProps) {
    this.requestAndRedraw().done();
  }

  async requestAndRedraw() {

    const chartScriptPromise = ChartClient.getChartScript(this.props.chartRequest.chartScript);
    const chartComponentModulePromise = ChartClient.getRegisteredChartScriptComponent(this.props.chartRequest.chartScript);

    const chartScript = await chartScriptPromise;
    const chartComponentModule = await chartComponentModulePromise();


    const data = this.props.data;
    data.parameters = ChartClient.API.getParameterWithDefault(this.props.chartRequest, chartScript);

    chartScript.columns.map((cc, i) => {
      if (!(data.columns as any)["c" + i])
        (data.columns as any)["c" + i] = { name: "c" + 1 };
    });

    this.setState({ chartComponent: chartComponentModule.default, chartScript });
  }


  handleDrillDown = (r: ChartRow) => {

    const cr = this.props.lastChartRequest!;

    if (cr.groupResults == false) {
      window.open(Navigator.navigateRoute(r.entity!));
    } else {
      const filters = cr.filterOptions.filter(a => !hasAggregate(a.token));

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
      <ErrorBoundary>
        {this.state.chartComponent && React.createElement(this.state.chartComponent, { data: this.props.data, onDrillDown: this.handleDrillDown })}
      </ErrorBoundary>
    );
  }
}
